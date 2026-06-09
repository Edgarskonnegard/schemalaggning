import { Fragment, useEffect, useMemo, useRef, useState } from "react";

import {
  createScheduleShift,
  generateStoreSchedule,
  getSchedules,
  publishSchedule,
  swapShiftEmployees,
  updateShift,
} from "../api/schedulesApi";
import {
  approveLeaveRequest,
  getScheduleLeaveBlocks,
  rejectLeaveRequest,
} from "../api/leaveRequestsApi";
import { getEmployees } from "../api/employeesApi";
import { getStores } from "../api/storesApi";
import ShiftNote from "../components/schedule/ShiftNote";
import Alert from "../components/ui/Alert";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import PageHeader from "../components/ui/PageHeader";
import Select from "../components/ui/Select";
import { getConsecutiveWorkdayWarnings } from "../utils/scheduleWarnings";
import "../components/calendar/Calendar.css";
import "./EditSchedulePage.css";

function toDateInputValue(date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function getDefaultStartDate() {
  const date = new Date();
  const day = date.getDay();
  const daysUntilMonday = (8 - day) % 7 || 7;
  date.setDate(date.getDate() + daysUntilMonday);
  return toDateInputValue(date);
}

function getDefaultEndDate(startDate) {
  const date = new Date(`${startDate}T00:00:00`);
  date.setDate(date.getDate() + 27);
  return toDateInputValue(date);
}

function addDaysToDateInput(value, days) {
  const date = new Date(`${value}T00:00:00`);
  date.setDate(date.getDate() + days);
  return toDateInputValue(date);
}

function getDaysBetween(start, end) {
  const startDate = new Date(`${start}T00:00:00`);
  const endDate = new Date(`${end}T00:00:00`);
  return Math.max(0, Math.round((endDate - startDate) / 86400000));
}

function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function getDayLabel(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    weekday: "short",
  });
}

function getMonthLabel(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    month: "short",
  });
}

function formatDate(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

function getDatesBetween(start, end) {
  if (!start || !end) {
    return [];
  }

  const dates = [];
  const current = new Date(`${start}T00:00:00`);
  const last = new Date(`${end}T00:00:00`);

  while (current <= last) {
    dates.push(toDateInputValue(current));
    current.setDate(current.getDate() + 1);
  }

  return dates;
}

function getFirstAvailableScheduleStart(
  schedules,
  storeId,
  requestedStart,
  durationInDays
) {
  if (!storeId || !requestedStart) {
    return requestedStart;
  }

  return schedules
    .filter(
      (schedule) =>
        schedule.storeId.toString() === storeId.toString() &&
        schedule.status === "Published" &&
        schedule.periodEnd >= requestedStart
    )
    .sort((a, b) => a.periodStart.localeCompare(b.periodStart))
    .reduce((start, schedule) => {
      const periodEnd = addDaysToDateInput(start, durationInDays);

      if (schedule.periodStart > periodEnd) {
        return start;
      }

      if (schedule.periodStart <= periodEnd && schedule.periodEnd >= start) {
        return addDaysToDateInput(schedule.periodEnd, 1);
      }

      return start;
    }, requestedStart);
}

function EditSchedulePage() {
  const initialStart = getDefaultStartDate();
  const calendarScrollRef = useRef(null);
  const panStateRef = useRef(null);
  const [stores, setStores] = useState([]);
  const [employees, setEmployees] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  const [leaveBlocks, setLeaveBlocks] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [draggedShift, setDraggedShift] = useState(null);
  const [draggedCoverageGap, setDraggedCoverageGap] = useState(null);
  const [isPanningSchedule, setIsPanningSchedule] = useState(false);
  const [isPublishModalOpen, setIsPublishModalOpen] = useState(false);
  const [leaveDecisionIds, setLeaveDecisionIds] = useState([]);
  const [rejectedLeaveReviewIds, setRejectedLeaveReviewIds] = useState([]);
  const [leaveReviewMessage, setLeaveReviewMessage] = useState("");
  const [editingShift, setEditingShift] = useState(null);
  const [shiftTimeForm, setShiftTimeForm] = useState({
    startTime: "",
    endTime: "",
  });
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    storeId: "",
    periodStart: initialStart,
    periodEnd: getDefaultEndDate(initialStart),
  });

  useEffect(() => {
    async function loadData() {
      setError("");
      setIsLoading(true);

      try {
        const [storesResult, schedulesResult, employeesResult] = await Promise.all([
          getStores(),
          getSchedules(),
          getEmployees(),
        ]);
        const defaultStoreId = storesResult[0]?.id?.toString() ?? "";
        const suggestedStart = getFirstAvailableScheduleStart(
          schedulesResult,
          defaultStoreId,
          initialStart,
          27
        );

        setStores(storesResult);
        setEmployees(employeesResult);
        setSchedules(schedulesResult);
        setForm((prev) => ({
          ...prev,
          storeId: defaultStoreId,
          periodStart: suggestedStart,
          periodEnd: getDefaultEndDate(suggestedStart),
        }));
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
      }
    }

    loadData();
  }, []);

  useEffect(() => {
    function handleWindowMouseMove(event) {
      const panState = panStateRef.current;
      const scrollElement = calendarScrollRef.current;

      if (!panState || !scrollElement) {
        return;
      }

      event.preventDefault();
      scrollElement.scrollLeft =
        panState.scrollLeft + panState.startX - event.clientX;
    }

    function handleWindowMouseUp() {
      panStateRef.current = null;
      setIsPanningSchedule(false);
      document.body.style.cursor = "";
    }

    window.addEventListener("mousemove", handleWindowMouseMove);
    window.addEventListener("mouseup", handleWindowMouseUp);

    return () => {
      window.removeEventListener("mousemove", handleWindowMouseMove);
      window.removeEventListener("mouseup", handleWindowMouseUp);
      document.body.style.cursor = "";
    };
  }, []);

  const dates = useMemo(() => {
    if (!selectedSchedule) {
      return [];
    }

    return getDatesBetween(selectedSchedule.periodStart, selectedSchedule.periodEnd);
  }, [selectedSchedule]);

  const storeEmployees = useMemo(() => {
    const storeId = selectedSchedule?.storeId ?? form.storeId;

    if (!storeId) {
      return [];
    }

    return employees
      .filter((employee) => employee.storeId.toString() === storeId.toString())
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [employees, form.storeId, selectedSchedule]);

  const employeesInSchedule = useMemo(() => {
    if (!selectedSchedule) {
      return [];
    }

    const employeeMap = selectedSchedule.shifts.reduce((map, shift) => {
      map.set(shift.employeeId, {
        id: shift.employeeId,
        name: shift.employeeName,
        roleName: shift.employeeRoleName,
      });
      return map;
    }, new Map());

    leaveBlocks.forEach((block) => {
      if (!employeeMap.has(block.employeeId)) {
        employeeMap.set(block.employeeId, {
          id: block.employeeId,
          name: block.employeeName,
          roleName: block.employeeRoleName,
        });
      }
    });

    storeEmployees.forEach((employee) => {
      if (!employeeMap.has(employee.id)) {
        employeeMap.set(employee.id, {
          id: employee.id,
          name: employee.name,
          roleName: employee.roleName,
        });
      }
    });

    return [...employeeMap.values()].sort((a, b) =>
      a.name.localeCompare(b.name)
    );
  }, [leaveBlocks, selectedSchedule, storeEmployees]);

  const shiftsByEmployeeAndDate = useMemo(() => {
    if (!selectedSchedule) {
      return {};
    }

    return selectedSchedule.shifts.reduce((groups, shift) => {
      const key = `${shift.employeeId}-${shift.date}`;
      groups[key] = groups[key] ?? [];
      groups[key].push(shift);
      return groups;
    }, {});
  }, [selectedSchedule]);

  const leaveBlocksByEmployeeAndDate = useMemo(() => {
    if (!selectedSchedule) {
      return {};
    }

    return leaveBlocks.reduce((groups, block) => {
      const start = block.startDate < selectedSchedule.periodStart
        ? selectedSchedule.periodStart
        : block.startDate;
      const end = block.endDate > selectedSchedule.periodEnd
        ? selectedSchedule.periodEnd
        : block.endDate;
      const current = new Date(`${start}T00:00:00`);
      const last = new Date(`${end}T00:00:00`);

      while (current <= last) {
        const date = toDateInputValue(current);
        const key = `${block.employeeId}-${date}`;
        groups[key] = groups[key] ?? [];
        groups[key].push(block);
        current.setDate(current.getDate() + 1);
      }

      return groups;
    }, {});
  }, [leaveBlocks, selectedSchedule]);

  const pendingLeaveBlocks = useMemo(
    () => leaveBlocks.filter((block) => block.status === "Pending"),
    [leaveBlocks]
  );

  const consecutiveWorkdayWarnings = useMemo(
    () =>
      getConsecutiveWorkdayWarnings(
        employeesInSchedule,
        dates,
        shiftsByEmployeeAndDate
      ),
    [dates, employeesInSchedule, shiftsByEmployeeAndDate]
  );

  const coverageGaps = selectedSchedule?.coverageGaps ?? [];
  const hasCoverageGaps = coverageGaps.length > 0;

  const coverageGapsByDate = useMemo(() => {
    return coverageGaps.reduce((groups, gap) => {
      groups[gap.date] = groups[gap.date] ?? [];
      groups[gap.date].push(gap);
      return groups;
    }, {});
  }, [coverageGaps]);

  const hasRejectedLeaveDuringReview = rejectedLeaveReviewIds.length > 0;

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  function handleStoreChange(storeId) {
    const durationInDays = getDaysBetween(form.periodStart, form.periodEnd);
    const suggestedStart = getFirstAvailableScheduleStart(
      schedules,
      storeId,
      form.periodStart,
      durationInDays
    );

    setForm((prev) => ({
      ...prev,
      storeId,
      periodStart: suggestedStart,
      periodEnd: getDefaultEndDate(suggestedStart),
    }));
  }

  function canDropShiftOnCell(targetDate) {
    return (
      Boolean(draggedShift) &&
      selectedSchedule?.status === "Draft" &&
      draggedShift.date === targetDate
    );
  }

  function canDropCoverageGapOnCell(employee, targetDate) {
    if (
      !draggedCoverageGap ||
      selectedSchedule?.status !== "Draft" ||
      draggedCoverageGap.date !== targetDate
    ) {
      return false;
    }

    const hasLeave =
      (leaveBlocksByEmployeeAndDate[`${employee.id}-${targetDate}`] ?? [])
        .length > 0;
    const hasShift =
      (shiftsByEmployeeAndDate[`${employee.id}-${targetDate}`] ?? []).length > 0;

    return !hasLeave && !hasShift;
  }

  async function handleGenerate(event) {
    event.preventDefault();

    if (!form.storeId) {
      setError("Välj butik först.");
      return;
    }

    setError("");
    setLeaveBlocks([]);
    setIsPublishModalOpen(false);
    setRejectedLeaveReviewIds([]);
    setLeaveReviewMessage("");
    setIsSaving(true);

    try {
      const schedule = await generateStoreSchedule(form.storeId, {
        periodStart: form.periodStart,
        periodEnd: form.periodEnd,
      });
      const blocks = await getScheduleLeaveBlocks(
        schedule.storeId,
        schedule.periodStart,
        schedule.periodEnd
      );

      setSelectedSchedule(schedule);
      setLeaveBlocks(blocks);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handlePublish() {
    if (!selectedSchedule) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await publishSchedule(selectedSchedule.id);
      setSelectedSchedule((prev) => ({
        ...prev,
        status: "Published",
        shifts: prev.shifts.map((shift) => ({
          ...shift,
          status: "Published",
        })),
      }));
      setSchedules((prev) => {
        const publishedSchedule = {
          ...selectedSchedule,
          status: "Published",
        };

        if (prev.some((schedule) => schedule.id === selectedSchedule.id)) {
          return prev.map((schedule) =>
            schedule.id === selectedSchedule.id ? publishedSchedule : schedule
          );
        }

        return [publishedSchedule, ...prev];
      });
      setIsPublishModalOpen(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  function handlePublishClick() {
    if (hasCoverageGaps) {
      setError("Alla bemanningsbehov måste vara placerade innan schemat godkänns.");
      return;
    }

    if (hasRejectedLeaveDuringReview) {
      setError(
        "Generera om schemautkastet innan publicering eftersom en ledighet nekades."
      );
      setIsPublishModalOpen(true);
      return;
    }

    if (pendingLeaveBlocks.length > 0) {
      setError("");
      setLeaveReviewMessage("");
      setIsPublishModalOpen(true);
      return;
    }

    handlePublish();
  }

  function getCoverageGapKey(gap) {
    return `${gap.date}-${gap.shiftTypeId}-${gap.startTime}-${gap.endTime}`;
  }

  async function handlePlaceCoverageGap(gap, employeeId) {
    if (!selectedSchedule) {
      return;
    }

    if (!employeeId) {
      setError("Välj en anställd att placera passet på.");
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      const updatedSchedule = await createScheduleShift(selectedSchedule.id, {
        employeeId: Number(employeeId),
        shiftTypeId: gap.shiftTypeId,
        date: gap.date,
        startTime: gap.startTime,
        endTime: gap.endTime,
      });

      setSelectedSchedule(updatedSchedule);
    } catch (err) {
      setError(err.message);
    } finally {
      setDraggedCoverageGap(null);
      setIsSaving(false);
    }
  }

  async function refreshLeaveBlocks(schedule = selectedSchedule) {
    if (!schedule) {
      return [];
    }

    const blocks = await getScheduleLeaveBlocks(
      schedule.storeId,
      schedule.periodStart,
      schedule.periodEnd
    );
    setLeaveBlocks(blocks);
    return blocks;
  }

  async function handleLeaveDecision(block, decision) {
    setError("");
    setLeaveReviewMessage("");
    setLeaveDecisionIds((prev) => [...prev, block.id]);

    try {
      if (decision === "approve") {
        await approveLeaveRequest(block.id);
        setLeaveReviewMessage(`${block.employeeName}s ledighet är godkänd.`);
      } else {
        await rejectLeaveRequest(block.id);
        setRejectedLeaveReviewIds((prev) => [...new Set([...prev, block.id])]);
        setLeaveReviewMessage(
          "Ledigheten nekades. Generera om schemautkastet innan du godkänner schemat, så passen kan placeras med den nya informationen."
        );
      }

      await refreshLeaveBlocks();
      window.dispatchEvent(new Event("approvals-updated"));
    } catch (err) {
      setError(err.message);
    } finally {
      setLeaveDecisionIds((prev) => prev.filter((id) => id !== block.id));
    }
  }

  async function handleReviewedPublish() {
    setError("");
    setLeaveReviewMessage("");

    if (hasRejectedLeaveDuringReview) {
      setLeaveReviewMessage(
        "Generera om schemautkastet innan publicering eftersom en ledighet nekades."
      );
      return;
    }

    try {
      const blocks = await refreshLeaveBlocks();
      const hasPending = blocks.some((block) => block.status === "Pending");

      if (hasPending) {
        setLeaveReviewMessage(
          "Alla ledighetsansökningar behöver hanteras innan schemat kan godkännas."
        );
        return;
      }

      await handlePublish();
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleDropShift(targetEmployee, targetDate) {
    if (draggedCoverageGap) {
      if (!canDropCoverageGapOnCell(targetEmployee, targetDate)) {
        setDraggedCoverageGap(null);
        return;
      }

      await handlePlaceCoverageGap(draggedCoverageGap, targetEmployee.id);
      return;
    }

    if (!draggedShift || selectedSchedule?.status !== "Draft") {
      setDraggedShift(null);
      return;
    }

    if (draggedShift.date !== targetDate) {
      setDraggedShift(null);
      return;
    }

    if (
      draggedShift.employeeId === targetEmployee.id &&
      draggedShift.date === targetDate
    ) {
      setDraggedShift(null);
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      const targetShifts =
        shiftsByEmployeeAndDate[`${targetEmployee.id}-${targetDate}`]?.filter(
          (shift) => shift.id !== draggedShift.id
        ) ?? [];
      const targetShift = targetShifts[0];

      if (targetShift) {
        const sourceEmployeeId = draggedShift.employeeId;
        const sourceEmployeeName = draggedShift.employeeName;
        const targetEmployeeId = targetShift.employeeId;
        const targetEmployeeName = targetShift.employeeName;

        await swapShiftEmployees(draggedShift.id, targetShift.id);

        setSelectedSchedule((prev) => ({
          ...prev,
          shifts: prev.shifts.map((shift) => {
            if (shift.id === draggedShift.id) {
              return {
                ...shift,
                employeeId: targetEmployeeId,
                employeeName: targetEmployeeName,
              };
            }

            if (shift.id === targetShift.id) {
              return {
                ...shift,
                employeeId: sourceEmployeeId,
                employeeName: sourceEmployeeName,
              };
            }

            return shift;
          }),
        }));
        return;
      }

      const updatedShift = {
        employeeId: targetEmployee.id,
        shiftTypeId: draggedShift.shiftTypeId,
        date: draggedShift.date,
        startTime: draggedShift.startTime,
        endTime: draggedShift.endTime,
      };

      await updateShift(draggedShift.id, updatedShift);

      setSelectedSchedule((prev) => ({
        ...prev,
        shifts: prev.shifts.map((shift) =>
          shift.id === draggedShift.id
            ? {
                ...shift,
                ...updatedShift,
                employeeName: targetEmployee.name,
              }
            : shift
        ),
      }));
    } catch (err) {
      setError(err.message);
    } finally {
      setDraggedShift(null);
      setIsSaving(false);
    }
  }

  function openShiftEditor(shift) {
    if (selectedSchedule?.status !== "Draft" || draggedShift) {
      return;
    }

    setError("");
    setEditingShift(shift);
    setShiftTimeForm({
      startTime: formatTime(shift.startTime),
      endTime: formatTime(shift.endTime),
    });
  }

  function closeShiftEditor() {
    if (isSaving) {
      return;
    }

    setEditingShift(null);
    setShiftTimeForm({
      startTime: "",
      endTime: "",
    });
  }

  function updateShiftTimeForm(field, value) {
    setShiftTimeForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  async function handleShiftTimeSubmit(event) {
    event.preventDefault();

    if (!editingShift) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      const updatedShift = {
        employeeId: editingShift.employeeId,
        shiftTypeId: editingShift.shiftTypeId,
        date: editingShift.date,
        startTime: shiftTimeForm.startTime,
        endTime: shiftTimeForm.endTime,
      };

      await updateShift(editingShift.id, updatedShift);

      setSelectedSchedule((prev) => ({
        ...prev,
        shifts: prev.shifts.map((shift) =>
          shift.id === editingShift.id
            ? {
                ...shift,
                ...updatedShift,
              }
            : shift
        ),
      }));
      setEditingShift(null);
      setShiftTimeForm({
        startTime: "",
        endTime: "",
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  function handleSchedulePanStart(event) {
    if (
      event.button !== 0 ||
      draggedShift ||
      draggedCoverageGap ||
      event.target.closest(".shift-note")
    ) {
      return;
    }

    const scrollElement = calendarScrollRef.current;
    if (!scrollElement) {
      return;
    }

    panStateRef.current = {
      startX: event.clientX,
      scrollLeft: scrollElement.scrollLeft,
    };

    event.preventDefault();
    document.body.style.cursor = "grabbing";
    setIsPanningSchedule(true);
  }

  return (
    <main className="schedule-draft-page">
      <PageHeader
        title="Skapa schema"
        description="Generera ett allmänt schema från godkända grundscheman."
      />

      <Alert>{error}</Alert>

      <section className="schedule-generator-card">
        <form className="schedule-generator-form" onSubmit={handleGenerate}>
          <Select
            label="Butik"
            value={form.storeId}
            onChange={(event) => handleStoreChange(event.target.value)}
          >
            <option value="">Välj butik</option>
            {stores.map((store) => (
              <option key={store.id} value={store.id}>
                {store.name}
              </option>
            ))}
          </Select>

          <Input
            label="Start"
            type="date"
            value={form.periodStart}
            onChange={(event) => {
              const start = event.target.value;
              setForm((prev) => ({
                ...prev,
                periodStart: start,
                periodEnd: getDefaultEndDate(start),
              }));
            }}
          />

          <Input
            label="Slut"
            type="date"
            value={form.periodEnd}
            onChange={(event) => updateForm("periodEnd", event.target.value)}
          />

          <Button type="submit" disabled={isSaving || isLoading}>
            {isSaving ? "Genererar..." : "Generera schemautkast"}
          </Button>
        </form>
      </section>

      <section className="schedule-review-card">
        <div className="schedule-review-header">
          <div>
            <h2>Schemautkast</h2>
            {selectedSchedule ? (
              <p>
                {selectedSchedule.storeName} · {selectedSchedule.periodStart} -{" "}
                {selectedSchedule.periodEnd} · {selectedSchedule.status}
              </p>
            ) : (
              <p>Generera ett utkast för att granska allas pass.</p>
            )}
          </div>

          {selectedSchedule && (
            <Button
              type="button"
              disabled={
                isSaving ||
                selectedSchedule.status !== "Draft" ||
                hasCoverageGaps
              }
              onClick={handlePublishClick}
            >
              Godkänn schema
            </Button>
          )}
        </div>

        {!selectedSchedule ? (
          <div className="schedule-empty-state">
            <p>Generera ett nytt schemautkast för att granska allas pass.</p>
          </div>
        ) : (
          <div
            className={`calendar schedule-review-calendar ${
              draggedShift || draggedCoverageGap
                ? "schedule-review-calendar--dragging"
                : ""
            } ${
              isPanningSchedule ? "schedule-review-calendar--panning" : ""
            }`}
          >
            <div className="schedule-review-shell">
              <div className="schedule-review-employees">
                <div className="calendar-corner">Anställd</div>

                {employeesInSchedule.map((employee) => (
                  <div className="calendar-employee" key={employee.id}>
                    <strong>{employee.name}</strong>
                    <span>{employee.roleName || "Roll saknas"}</span>
                  </div>
                ))}
              </div>

              <div
                className="calendar-scroll"
                ref={calendarScrollRef}
                onMouseDown={handleSchedulePanStart}
              >
                <div
                  className="schedule-date-grid"
                  style={{
                    gridTemplateColumns: `repeat(${dates.length}, 120px)`,
                    gridTemplateRows: `var(--schedule-header-height) repeat(${employeesInSchedule.length}, var(--schedule-row-height))`,
                  }}
                >
                  {dates.map((date) => {
                    const isDropColumn =
                      draggedShift?.date === date ||
                      draggedCoverageGap?.date === date;
                    const hasDateCoverageGap = Boolean(coverageGapsByDate[date]);

                    return (
                      <div
                        key={date}
                        className={`calendar-day-header ${
                          isDropColumn ? "schedule-drop-column" : ""
                        } ${
                          hasDateCoverageGap ? "schedule-coverage-gap-column" : ""
                        } ${
                          draggedShift && !isDropColumn
                            ? "schedule-dimmed-column"
                            : ""
                        }`}
                      >
                        <span className="calendar-day-name">
                          {getDayLabel(date)}
                        </span>
                        <strong>{new Date(`${date}T00:00:00`).getDate()}</strong>
                        <span className="calendar-month-name">
                          {getMonthLabel(date)}
                        </span>
                      </div>
                    );
                  })}

                  {employeesInSchedule.map((employee) => (
                    <Fragment key={employee.id}>
                      {dates.map((date) => {
                        const shifts =
                          shiftsByEmployeeAndDate[`${employee.id}-${date}`] ??
                          [];
                        const blocks =
                          leaveBlocksByEmployeeAndDate[`${employee.id}-${date}`] ??
                          [];
                        const consecutiveWarning =
                          consecutiveWorkdayWarnings[`${employee.id}-${date}`];
                        const canDropHere = canDropShiftOnCell(date);
                        const canDropGapHere = canDropCoverageGapOnCell(
                          employee,
                          date
                        );
                        const isInvalidGapTarget =
                          Boolean(draggedCoverageGap) &&
                          draggedCoverageGap.date === date &&
                          !canDropGapHere;
                        const hasDateCoverageGap = Boolean(coverageGapsByDate[date]);

                        return (
                          <div
                            key={`${employee.id}-${date}`}
                            className={`calendar-cell ${
                              blocks.length > 0 ? "schedule-leave-cell" : ""
                            } ${
                              consecutiveWarning
                                ? "schedule-consecutive-warning-cell"
                                : ""
                            } ${
                              hasDateCoverageGap ? "schedule-coverage-gap-cell" : ""
                            } ${
                              canDropHere || canDropGapHere
                                ? "schedule-drop-target"
                                : ""
                            } ${
                              isInvalidGapTarget
                                ? "schedule-invalid-drop-target"
                                : ""
                            } ${
                              (draggedShift && !canDropHere) ||
                              (draggedCoverageGap && !canDropGapHere)
                                ? "schedule-dimmed-column"
                                : ""
                            }`}
                            onDragOver={(event) => {
                              if (canDropHere || canDropGapHere) {
                                event.preventDefault();
                              }
                            }}
                            onDrop={() => handleDropShift(employee, date)}
                          >
                            {blocks.map((block) => (
                              <div
                                className={`schedule-leave-note schedule-leave-note-${block.status.toLowerCase()}`}
                                key={block.id}
                              >
                                <strong>Ledig</strong>
                                <small>
                                  {block.status === "Pending"
                                    ? "Väntar"
                                    : "Godkänd"}
                                </small>
                              </div>
                            ))}

                            {shifts.length === 0 ? (
                              blocks.length === 0 && (
                                <span className="calendar-empty">Ledig</span>
                              )
                            ) : (
                              <>
                                {consecutiveWarning && (
                                  <span className="schedule-rule-warning">
                                    {consecutiveWarning.runLength} dagar i rad
                                  </span>
                                )}
                                {shifts.map((shift) => (
                                  <ShiftNote
                                    draggable={selectedSchedule.status === "Draft"}
                                    key={shift.id}
                                    title={shift.shiftTypeName}
                                    time={`${formatTime(shift.startTime)}-${formatTime(
                                      shift.endTime
                                    )}`}
                                    onClick={() => openShiftEditor(shift)}
                                    onDragStart={() => {
                                      setDraggedCoverageGap(null);
                                      setDraggedShift(shift);
                                    }}
                                    onDragEnd={() => setDraggedShift(null)}
                                    onKeyDown={(event) => {
                                      if (
                                        event.key === "Enter" ||
                                        event.key === " "
                                      ) {
                                        event.preventDefault();
                                        openShiftEditor(shift);
                                      }
                                    }}
                                  />
                                ))}
                              </>
                            )}
                          </div>
                        );
                      })}
                    </Fragment>
                  ))}
                </div>
              </div>
            </div>

            {hasCoverageGaps && (
              <section className="schedule-coverage-gaps">
                <div>
                  <h3>Bemanningsbehov saknas</h3>
                  <p>
                    Dessa pass från bemanningsbehovet finns inte i utkastet.
                    Dra ett passkort till rätt datumkolumn hos en anställd.
                  </p>
                </div>

                <div className="schedule-coverage-gap-list">
                  {coverageGaps.map((gap) => (
                    <ShiftNote
                      className="schedule-coverage-gap-card"
                      draggable={selectedSchedule.status === "Draft" && !isSaving}
                      key={getCoverageGapKey(gap)}
                      title={gap.shiftTypeName}
                      time={`${formatTime(gap.startTime)}-${formatTime(gap.endTime)}`}
                      onDragStart={() => {
                        setDraggedShift(null);
                        setDraggedCoverageGap(gap);
                      }}
                      onDragEnd={() => setDraggedCoverageGap(null)}
                    >
                      <span>{formatDate(gap.date)}</span>
                      <small>
                        Saknas {gap.missingCount} av {gap.requiredCount}
                      </small>
                    </ShiftNote>
                  ))}
                </div>
              </section>
            )}
          </div>
        )}
      </section>

      {editingShift && (
        <div className="schedule-modal-backdrop" onClick={closeShiftEditor}>
          <section
            className="schedule-modal"
            aria-labelledby="schedule-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="schedule-modal-header">
              <div>
                <h2 id="schedule-modal-title">Redigera pass</h2>
                <p>
                  {editingShift.employeeName} · {editingShift.shiftTypeName} ·{" "}
                  {editingShift.date}
                </p>
              </div>

              <button
                aria-label="Stäng"
                className="schedule-modal-close"
                type="button"
                onClick={closeShiftEditor}
              >
                ×
              </button>
            </div>

            <form className="schedule-modal-form" onSubmit={handleShiftTimeSubmit}>
              <Input
                label="Starttid"
                type="time"
                value={shiftTimeForm.startTime}
                onChange={(event) =>
                  updateShiftTimeForm("startTime", event.target.value)
                }
              />

              <Input
                label="Sluttid"
                type="time"
                value={shiftTimeForm.endTime}
                onChange={(event) =>
                  updateShiftTimeForm("endTime", event.target.value)
                }
              />

              <div className="schedule-modal-actions">
                <Button type="button" onClick={closeShiftEditor}>
                  Avbryt
                </Button>
                <Button type="submit" disabled={isSaving}>
                  {isSaving ? "Sparar..." : "Spara tid"}
                </Button>
              </div>
            </form>
          </section>
        </div>
      )}

      {isPublishModalOpen && (
        <div
          className="schedule-modal-backdrop"
          onClick={() => {
            if (!isSaving) {
              setIsPublishModalOpen(false);
            }
          }}
        >
          <section
            className="schedule-modal schedule-publish-modal"
            aria-labelledby="schedule-publish-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="schedule-modal-header">
              <div>
                <h2 id="schedule-publish-modal-title">
                  Granska ledigheter
                </h2>
                <p>
                  Hantera väntande ledighetsansökningar innan schemat godkänns.
                </p>
              </div>

              <button
                aria-label="Stäng"
                className="schedule-modal-close"
                type="button"
                disabled={isSaving}
                onClick={() => setIsPublishModalOpen(false)}
              >
                ×
              </button>
            </div>

            {leaveReviewMessage && (
              <div className="schedule-publish-message">
                {leaveReviewMessage}
              </div>
            )}

            {pendingLeaveBlocks.length === 0 ? (
              <div className="schedule-publish-ready">
                Alla ledigheter i utkastet är hanterade.
              </div>
            ) : (
              <div className="schedule-publish-leave-list">
                {pendingLeaveBlocks.map((block) => {
                  const isDeciding = leaveDecisionIds.includes(block.id);

                  return (
                    <article className="schedule-publish-leave-item" key={block.id}>
                      <div>
                        <strong>{block.employeeName}</strong>
                        <span>{block.employeeRoleName || "Roll saknas"}</span>
                      </div>

                      <p>
                        {formatDate(block.startDate)} - {formatDate(block.endDate)}
                      </p>

                      <small>
                        {block.requestedDays} semesterdagar
                        {block.reason ? ` · ${block.reason}` : ""}
                      </small>

                      <div className="schedule-publish-leave-actions">
                        <Button
                          type="button"
                          variant="secondary"
                          disabled={isSaving || isDeciding}
                          onClick={() => handleLeaveDecision(block, "approve")}
                        >
                          {isDeciding ? "Hanterar..." : "Godkänn"}
                        </Button>
                        <Button
                          type="button"
                          variant="danger"
                          disabled={isSaving || isDeciding}
                          onClick={() => handleLeaveDecision(block, "reject")}
                        >
                          Neka
                        </Button>
                      </div>
                    </article>
                  );
                })}
              </div>
            )}

            <div className="schedule-modal-actions">
              <Button
                type="button"
                variant="secondary"
                disabled={isSaving}
                onClick={() => setIsPublishModalOpen(false)}
              >
                Avbryt
              </Button>
              <Button
                type="button"
                disabled={
                  isSaving ||
                  pendingLeaveBlocks.length > 0 ||
                  hasRejectedLeaveDuringReview
                }
                onClick={handleReviewedPublish}
              >
                {isSaving ? "Godkänner..." : "Godkänn schema"}
              </Button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

export default EditSchedulePage;
