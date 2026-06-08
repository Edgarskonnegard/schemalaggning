import { Fragment, useEffect, useMemo, useRef, useState } from "react";

import {
  generateStoreSchedule,
  publishSchedule,
  swapShiftEmployees,
  updateShift,
} from "../api/schedulesApi";
import { getScheduleLeaveBlocks } from "../api/leaveRequestsApi";
import { getStores } from "../api/storesApi";
import ShiftNote from "../components/schedule/ShiftNote";
import Alert from "../components/ui/Alert";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import PageHeader from "../components/ui/PageHeader";
import Select from "../components/ui/Select";
import "../components/calendar/Calendar.css";
import "./EditSchedulePage.css";

function toDateInputValue(date) {
  return date.toISOString().slice(0, 10);
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

function EditSchedulePage() {
  const initialStart = getDefaultStartDate();
  const calendarScrollRef = useRef(null);
  const panStateRef = useRef(null);
  const [stores, setStores] = useState([]);
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  const [leaveBlocks, setLeaveBlocks] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [draggedShift, setDraggedShift] = useState(null);
  const [isPanningSchedule, setIsPanningSchedule] = useState(false);
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
        const storesResult = await getStores();

        setStores(storesResult);
        setForm((prev) => ({
          ...prev,
          storeId: storesResult[0]?.id?.toString() ?? "",
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

    return [...employeeMap.values()].sort((a, b) =>
      a.name.localeCompare(b.name)
    );
  }, [leaveBlocks, selectedSchedule]);

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

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  function canDropShiftOnCell(targetDate) {
    return (
      Boolean(draggedShift) &&
      selectedSchedule?.status === "Draft" &&
      draggedShift.date === targetDate
    );
  }

  async function handleGenerate(event) {
    event.preventDefault();

    if (!form.storeId) {
      setError("Välj butik först.");
      return;
    }

    setError("");
    setLeaveBlocks([]);
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
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDropShift(targetEmployee, targetDate) {
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
            onChange={(event) => updateForm("storeId", event.target.value)}
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
              disabled={isSaving || selectedSchedule.status !== "Draft"}
              onClick={handlePublish}
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
              draggedShift ? "schedule-review-calendar--dragging" : ""
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
                  }}
                >
                  {dates.map((date) => {
                    const isDropColumn = draggedShift?.date === date;

                    return (
                      <div
                        key={date}
                        className={`calendar-day-header ${
                          isDropColumn ? "schedule-drop-column" : ""
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
                        const canDropHere = canDropShiftOnCell(date);

                        return (
                          <div
                            key={`${employee.id}-${date}`}
                            className={`calendar-cell ${
                              blocks.length > 0 ? "schedule-leave-cell" : ""
                            } ${
                              canDropHere ? "schedule-drop-target" : ""
                            } ${
                              draggedShift && !canDropHere
                                ? "schedule-dimmed-column"
                                : ""
                            }`}
                            onDragOver={(event) => {
                              if (canDropHere) {
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
                              shifts.map((shift) => (
                                <ShiftNote
                                  draggable={selectedSchedule.status === "Draft"}
                                  key={shift.id}
                                  title={shift.shiftTypeName}
                                  time={`${formatTime(shift.startTime)}-${formatTime(
                                    shift.endTime
                                  )}`}
                                  onClick={() => openShiftEditor(shift)}
                                  onDragStart={() => setDraggedShift(shift)}
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
                              ))
                            )}
                          </div>
                        );
                      })}
                    </Fragment>
                  ))}
                </div>
              </div>
            </div>
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
    </main>
  );
}

export default EditSchedulePage;
