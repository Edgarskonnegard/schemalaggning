import { Fragment, useEffect, useMemo, useRef, useState } from "react";

import { getScheduleLeaveBlocks } from "../../api/leaveRequestsApi";
import { getSchedule, getSchedules, updateShift } from "../../api/schedulesApi";
import { getConsecutiveWorkdayWarnings } from "../../utils/scheduleWarnings";
import ShiftNote from "../schedule/ShiftNote";
import "./Calendar.css";

const MAX_SCHEDULE_DAYS_FROM_TODAY = 90;

function toDateInputValue(date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDate(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
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

  const days = [];
  const current = new Date(`${start}T00:00:00`);
  const last = new Date(`${end}T00:00:00`);

  while (current <= last) {
    days.push(toDateInputValue(current));
    current.setDate(current.getDate() + 1);
  }

  return days;
}

function addDaysToDateInput(value, days) {
  const date = new Date(`${value}T00:00:00`);
  date.setDate(date.getDate() + days);
  return toDateInputValue(date);
}

function isDateWithinSchedule(schedule, date) {
  return schedule.periodStart <= date && schedule.periodEnd >= date;
}

function getActivePublishedSchedule(schedules) {
  const today = toDateInputValue(new Date());
  return schedules.find(
    (schedule) =>
      schedule.status === "Published" && isDateWithinSchedule(schedule, today)
  );
}

function getDisplaySeedSchedule(schedules, today) {
  const publishedSchedules = schedules
    .filter((schedule) => schedule.status === "Published")
    .sort((a, b) => a.periodStart.localeCompare(b.periodStart));

  return (
    publishedSchedules.find((schedule) => isDateWithinSchedule(schedule, today)) ||
    publishedSchedules.find((schedule) => schedule.periodStart > today) ||
    publishedSchedules[publishedSchedules.length - 1] ||
    null
  );
}

function getDisplayScheduleSummaries(schedules, seedSchedule, today, maxEnd) {
  if (!seedSchedule) {
    return [];
  }

  const windowStart = seedSchedule.periodStart;

  return schedules
    .filter(
      (schedule) =>
        schedule.status === "Published" &&
        schedule.storeId === seedSchedule.storeId &&
        schedule.periodEnd >= windowStart &&
        schedule.periodStart <= maxEnd
    )
    .sort((a, b) => a.periodStart.localeCompare(b.periodStart));
}

function buildContinuousSchedule(schedules, displayStart, maxEnd) {
  if (schedules.length === 0) {
    return null;
  }

  const coveredDates = new Set();
  const shifts = [];
  let displayEnd = displayStart;

  schedules.forEach((schedule) => {
    const scheduleStart = schedule.periodStart < displayStart
      ? displayStart
      : schedule.periodStart;
    const scheduleEnd = schedule.periodEnd > maxEnd ? maxEnd : schedule.periodEnd;

    if (scheduleStart > scheduleEnd) {
      return;
    }

    for (const date of getDatesBetween(scheduleStart, scheduleEnd)) {
      if (!coveredDates.has(date)) {
        coveredDates.add(date);
        displayEnd = date > displayEnd ? date : displayEnd;
      }
    }

    schedule.shifts.forEach((shift) => {
      if (
        shift.date >= scheduleStart &&
        shift.date <= scheduleEnd &&
        !shifts.some((existingShift) => existingShift.id === shift.id)
      ) {
        const dateBelongsToEarlierSchedule = schedules.some(
          (otherSchedule) =>
            otherSchedule.id !== schedule.id &&
            otherSchedule.periodStart < schedule.periodStart &&
            otherSchedule.periodStart <= shift.date &&
            otherSchedule.periodEnd >= shift.date
        );

        if (!dateBelongsToEarlierSchedule) {
          shifts.push(shift);
        }
      }
    });
  });

  return {
    id: "continuous",
    name: "Publicerat schema",
    storeId: schedules[0].storeId,
    storeName: schedules[0].storeName,
    periodStart: displayStart,
    periodEnd: displayEnd,
    status: "Published",
    shifts,
  };
}

function toShiftUpdateDto(shift) {
  return {
    employeeId: shift.employeeId,
    shiftTypeId: shift.shiftTypeId,
    date: shift.date,
    startTime: formatTime(shift.startTime),
    endTime: formatTime(shift.endTime),
  };
}

function Calendar() {
  const calendarScrollRef = useRef(null);
  const panStateRef = useRef(null);
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  const [leaveBlocks, setLeaveBlocks] = useState([]);
  const [employees, setEmployees] = useState([]);
  const [pendingShiftUpdates, setPendingShiftUpdates] = useState({});
  const [draggedShift, setDraggedShift] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSchedule, setIsLoadingSchedule] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isPanning, setIsPanning] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadSchedules() {
      setError("");
      setIsLoading(true);

      try {
        const result = await getSchedules();
        const today = toDateInputValue(new Date());
        const maxEnd = addDaysToDateInput(today, MAX_SCHEDULE_DAYS_FROM_TODAY);
        const seedSchedule =
          getActivePublishedSchedule(result) ||
          getDisplaySeedSchedule(result, today);
        const displaySummaries = getDisplayScheduleSummaries(
          result,
          seedSchedule,
          today,
          maxEnd
        );

        if (displaySummaries.length === 0 || !seedSchedule) {
          setSelectedSchedule(null);
          setLeaveBlocks([]);
          setEmployees([]);
          return;
        }

        setIsLoadingSchedule(true);

        const fullSchedules = await Promise.all(
          displaySummaries.map((schedule) => getSchedule(schedule.id))
        );
        const continuousSchedule = buildContinuousSchedule(
          fullSchedules,
          seedSchedule.periodStart,
          maxEnd
        );

        if (!continuousSchedule) {
          setSelectedSchedule(null);
          setLeaveBlocks([]);
          setEmployees([]);
          return;
        }

        const blocks = await getScheduleLeaveBlocks(
          continuousSchedule.storeId,
          continuousSchedule.periodStart,
          continuousSchedule.periodEnd
        );
        const employeeMap = continuousSchedule.shifts.reduce((map, shift) => {
          map.set(shift.employeeId, {
            id: shift.employeeId,
            name: shift.employeeName,
            roleName: shift.employeeRoleName,
          });
          return map;
        }, new Map());

        blocks.forEach((block) => {
          if (!employeeMap.has(block.employeeId)) {
            employeeMap.set(block.employeeId, {
              id: block.employeeId,
              name: block.employeeName,
              roleName: block.employeeRoleName,
            });
          }
        });

        setSelectedSchedule(continuousSchedule);
        setLeaveBlocks(blocks);
        setEmployees(
          [...employeeMap.values()].sort((a, b) => a.name.localeCompare(b.name))
        );
        setPendingShiftUpdates({});
        setDraggedShift(null);
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
        setIsLoadingSchedule(false);
      }
    }

    loadSchedules();
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
      setIsPanning(false);
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
      const start =
        block.startDate < selectedSchedule.periodStart
          ? selectedSchedule.periodStart
          : block.startDate;
      const end =
        block.endDate > selectedSchedule.periodEnd
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

  const pendingCount = Object.keys(pendingShiftUpdates).length;

  const consecutiveWorkdayWarnings = useMemo(
    () =>
      getConsecutiveWorkdayWarnings(
        employees,
        dates,
        shiftsByEmployeeAndDate
      ),
    [dates, employees, shiftsByEmployeeAndDate]
  );

  function handlePanStart(event) {
    if (event.button !== 0 || event.target.closest(".shift-note")) {
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
    setIsPanning(true);
  }

  function canDropShiftOnCell(date) {
    return Boolean(draggedShift) && draggedShift.date === date;
  }

  function handleDropShift(targetEmployee, date) {
    if (
      !selectedSchedule ||
      !canDropShiftOnCell(date) ||
      draggedShift.employeeId === targetEmployee.id
    ) {
      setDraggedShift(null);
      return;
    }

    const sourceEmployee = employees.find(
      (employee) => employee.id === draggedShift.employeeId
    );
    const targetShifts =
      shiftsByEmployeeAndDate[`${targetEmployee.id}-${date}`]?.filter(
        (shift) => shift.id !== draggedShift.id
      ) ?? [];
    const targetShift = targetShifts[0];

    const nextShifts = selectedSchedule.shifts.map((shift) => {
      if (shift.id === draggedShift.id) {
        return {
          ...shift,
          employeeId: targetEmployee.id,
          employeeName: targetEmployee.name,
          employeeRoleName: targetEmployee.roleName,
        };
      }

      if (targetShift && shift.id === targetShift.id && sourceEmployee) {
        return {
          ...shift,
          employeeId: sourceEmployee.id,
          employeeName: sourceEmployee.name,
          employeeRoleName: sourceEmployee.roleName,
        };
      }

      return shift;
    });

    const movedShift = nextShifts.find((shift) => shift.id === draggedShift.id);
    const swappedShift = targetShift
      ? nextShifts.find((shift) => shift.id === targetShift.id)
      : null;

    setSelectedSchedule((prev) => ({
      ...prev,
      shifts: nextShifts,
    }));

    setPendingShiftUpdates((prev) => {
      const nextUpdates = { ...prev };

      if (movedShift) {
        nextUpdates[movedShift.id] = toShiftUpdateDto(movedShift);
      }

      if (swappedShift) {
        nextUpdates[swappedShift.id] = toShiftUpdateDto(swappedShift);
      }

      return nextUpdates;
    });

    setDraggedShift(null);
  }

  async function handleSaveChanges() {
    const updates = Object.entries(pendingShiftUpdates);

    if (updates.length === 0) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await Promise.all(
        updates.map(([shiftId, shift]) => updateShift(shiftId, shift))
      );
      setPendingShiftUpdates({});
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section className="calendar">
      <header className="calendar-header">
        <div>
          <h2>
            {selectedSchedule
              ? `${selectedSchedule.name} · ${selectedSchedule.storeName}`
              : "Aktivt schema"}
          </h2>
          <p>
            {selectedSchedule
              ? `${formatDate(selectedSchedule.periodStart)} - ${formatDate(
                  selectedSchedule.periodEnd
                )}`
              : "Publicera ett schema för att visa det här."}
          </p>
        </div>

        {selectedSchedule && (
          <div className="calendar-actions">
            {pendingCount > 0 && (
              <span>{pendingCount} osparade ändringar</span>
            )}
            <button
              type="button"
              disabled={pendingCount === 0 || isSaving}
              onClick={handleSaveChanges}
            >
              {isSaving ? "Sparar..." : "Spara ändringar"}
            </button>
          </div>
        )}
      </header>

      {error && <p className="calendar-error">{error}</p>}

      {isLoading || isLoadingSchedule ? (
        <div className="calendar-empty-state">Laddar schema...</div>
      ) : !selectedSchedule ? (
        <div className="calendar-empty-state">
          Inget publicerat schema finns ännu. Godkänn ett schema först.
        </div>
      ) : employees.length === 0 ? (
        <div className="calendar-empty-state">
          Schemat finns, men innehåller inga pass.
        </div>
      ) : (
      <div
        className={`calendar-scroll ${isPanning ? "calendar-scroll-panning" : ""}`}
        ref={calendarScrollRef}
        onMouseDown={handlePanStart}
      >
        <div
          className="calendar-grid"
          style={{
            gridTemplateColumns: `180px repeat(${dates.length}, 120px)`,
          }}
        >
          <div className="calendar-corner">Anställd</div>

          {dates.map((date) => (
            <div
              key={date}
              className={`calendar-day-header ${
                draggedShift?.date === date ? "calendar-drop-column" : ""
              }`}
            >
              <span className="calendar-day-name">{getDayLabel(date)}</span>
              <strong>{new Date(`${date}T00:00:00`).getDate()}</strong>
              <span className="calendar-month-name">{getMonthLabel(date)}</span>
            </div>
          ))}

          {employees.map((employee) => (
            <Fragment key={employee.id}>
              <div key={`${employee.id}-info`} className="calendar-employee">
                <strong>{employee.name}</strong>
                <span>{employee.roleName || "Roll saknas"}</span>
              </div>

              {dates.map((date) => {
                const shifts =
                  shiftsByEmployeeAndDate[`${employee.id}-${date}`] ?? [];
                const blocks =
                  leaveBlocksByEmployeeAndDate[`${employee.id}-${date}`] ?? [];
                const consecutiveWarning =
                  consecutiveWorkdayWarnings[`${employee.id}-${date}`];

                return (
                  <div
                    key={`${employee.id}-${date}`}
                    className={`calendar-cell ${
                      blocks.length > 0 ? "calendar-leave-cell" : ""
                    } ${
                      canDropShiftOnCell(date) ? "calendar-drop-target" : ""
                    } ${
                      consecutiveWarning
                        ? "calendar-consecutive-warning-cell"
                        : ""
                    }`}
                    onDragOver={(event) => {
                      if (canDropShiftOnCell(date)) {
                        event.preventDefault();
                      }
                    }}
                    onDrop={() => handleDropShift(employee, date)}
                  >
                    {blocks.map((block) => (
                      <div
                        className={`calendar-leave-note calendar-leave-note-${block.status.toLowerCase()}`}
                        key={block.id}
                      >
                        <strong>Ledig</strong>
                        <small>
                          {block.status === "Pending" ? "Väntar" : "Godkänd"}
                        </small>
                      </div>
                    ))}

                    {shifts.length > 0 ? (
                      <>
                        {consecutiveWarning && (
                          <span className="calendar-rule-warning">
                            {consecutiveWarning.runLength} dagar i rad
                          </span>
                        )}
                        {shifts.map((shift) => (
                          <ShiftNote
                            draggable
                            key={shift.id}
                            title={shift.shiftTypeName}
                            time={`${formatTime(shift.startTime)}-${formatTime(
                              shift.endTime
                            )}`}
                            className={`calendar-shift-note ${
                              pendingShiftUpdates[shift.id]
                                ? "calendar-shift-note-pending"
                                : ""
                            }`}
                            onDragStart={() => setDraggedShift(shift)}
                            onDragEnd={() => setDraggedShift(null)}
                          />
                        ))}
                      </>
                    ) : (
                      blocks.length === 0 && (
                        <span className="calendar-empty">Ledig</span>
                      )
                    )}
                  </div>
                );
              })}
            </Fragment>
          ))}
        </div>
      </div>
      )}
    </section>
  );
}

export default Calendar;
