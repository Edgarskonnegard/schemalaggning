import { Fragment, useEffect, useMemo, useRef, useState } from "react";

import { getSchedule, getSchedules, updateShift } from "../../api/schedulesApi";
import ShiftNote from "../schedule/ShiftNote";
import "./Calendar.css";

function toDateInputValue(date) {
  return date.toISOString().slice(0, 10);
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

function isDateWithinSchedule(schedule, date) {
  return schedule.periodStart <= date && schedule.periodEnd >= date;
}

function getActivePublishedSchedule(schedules) {
  const today = toDateInputValue(new Date());
  const publishedSchedules = schedules
    .filter((schedule) => schedule.status === "Published")
    .sort((a, b) => new Date(b.periodStart) - new Date(a.periodStart));

  return (
    publishedSchedules.find((schedule) => isDateWithinSchedule(schedule, today)) ||
    publishedSchedules[0]
  );
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
  const [selectedScheduleId, setSelectedScheduleId] = useState("");
  const [selectedSchedule, setSelectedSchedule] = useState(null);
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
        const activeSchedule = getActivePublishedSchedule(result);
        setSelectedScheduleId(activeSchedule?.id?.toString() || "");
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
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

  useEffect(() => {
    if (!selectedScheduleId) {
      setSelectedSchedule(null);
      return;
    }

    async function loadSchedule() {
      setError("");
      setIsLoadingSchedule(true);

      try {
        const schedule = await getSchedule(selectedScheduleId);
        const employeeMap = schedule.shifts.reduce((map, shift) => {
          map.set(shift.employeeId, {
            id: shift.employeeId,
            name: shift.employeeName,
            roleName: shift.employeeRoleName,
          });
          return map;
        }, new Map());

        setSelectedSchedule(schedule);
        setEmployees(
          [...employeeMap.values()].sort((a, b) => a.name.localeCompare(b.name))
        );
        setPendingShiftUpdates({});
        setDraggedShift(null);
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoadingSchedule(false);
      }
    }

    loadSchedule();
  }, [selectedScheduleId]);

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

  const pendingCount = Object.keys(pendingShiftUpdates).length;

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

                return (
                  <div
                    key={`${employee.id}-${date}`}
                    className={`calendar-cell ${
                      canDropShiftOnCell(date) ? "calendar-drop-target" : ""
                    }`}
                    onDragOver={(event) => {
                      if (canDropShiftOnCell(date)) {
                        event.preventDefault();
                      }
                    }}
                    onDrop={() => handleDropShift(employee, date)}
                  >
                    {shifts.length > 0 ? (
                      shifts.map((shift) => (
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
                      ))
                    ) : (
                      <span className="calendar-empty">Ledig</span>
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
