import { useMemo, useState } from "react";
import "./EmployeeBaseSchedule.css";

const cycleWeeks = [
  { id: 1, label: "1" },
  { id: 2, label: "2" },
  { id: 3, label: "3" },
  { id: 4, label: "4" },
];

const weekDays = [
  { value: 1, id: "Monday", label: "Mån", fullLabel: "Måndag" },
  { value: 2, id: "Tuesday", label: "Tis", fullLabel: "Tisdag" },
  { value: 3, id: "Wednesday", label: "Ons", fullLabel: "Onsdag" },
  { value: 4, id: "Thursday", label: "Tor", fullLabel: "Torsdag" },
  { value: 5, id: "Friday", label: "Fre", fullLabel: "Fredag" },
  { value: 6, id: "Saturday", label: "Lör", fullLabel: "Lördag" },
  { value: 0, id: "Sunday", label: "Sön", fullLabel: "Söndag" },
];

function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function normalizeDayOfWeek(dayOfWeek) {
  if (typeof dayOfWeek === "number") {
    return weekDays.find((day) => day.value === dayOfWeek)?.id;
  }

  return dayOfWeek;
}

function EmployeeBaseSchedule({ baseSchedule, shiftTypes, onChange }) {
  const [selectedShiftTypeId, setSelectedShiftTypeId] = useState("");
  const [draggedRule, setDraggedRule] = useState(null);

  const normalizedRules = useMemo(() => {
    return baseSchedule.map((rule) => ({
      ...rule,
      dayOfWeek: normalizeDayOfWeek(rule.dayOfWeek),
    }));
  }, [baseSchedule]);

  function getRule(weekInCycle, dayOfWeek) {
    return normalizedRules.find(
      (rule) =>
        rule.weekInCycle === weekInCycle && rule.dayOfWeek === dayOfWeek
    );
  }

  function getShiftType(shiftTypeId) {
    return shiftTypes.find((shiftType) => shiftType.id === shiftTypeId);
  }

  function handleCellClick(weekInCycle, day) {
    const existingRule = getRule(weekInCycle, day.id);

    if (existingRule || !selectedShiftTypeId) {
      return;
    }

    onChange(weekInCycle, day.value, selectedShiftTypeId);
  }

  function handleDrop(targetWeekInCycle, targetDay) {
    if (!draggedRule) {
      return;
    }

    if (
      draggedRule.weekInCycle === targetWeekInCycle &&
      draggedRule.dayOfWeek === targetDay.id
    ) {
      setDraggedRule(null);
      return;
    }

    onChange([
      {
        weekInCycle: draggedRule.weekInCycle,
        dayOfWeek: draggedRule.dayOfWeekValue,
        shiftTypeId: "",
      },
      {
        weekInCycle: targetWeekInCycle,
        dayOfWeek: targetDay.value,
        shiftTypeId: draggedRule.shiftTypeId,
      },
    ]);
    setDraggedRule(null);
  }

  return (
    <section className="base-schedule-card">
      <div className="base-schedule-header">
        <div>
          <h2>Grundschema</h2>
          <p>Placera pass i den anställdas fyraveckorscykel.</p>
        </div>

        <div className="schedule-tool">
          <label>Pass att lägga till</label>
          <select
            value={selectedShiftTypeId}
            onChange={(e) => setSelectedShiftTypeId(e.target.value)}
          >
            <option value="">Välj passtyp</option>
            {shiftTypes.map((shiftType) => (
              <option key={shiftType.id} value={shiftType.id}>
                {shiftType.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="schedule-board">
        <div className="schedule-corner" />

        {weekDays.map((day) => (
          <div key={day.id} className="schedule-day-heading">
            <span>{day.label}</span>
            <strong>{day.fullLabel}</strong>
          </div>
        ))}

        {cycleWeeks.map((week) => (
          <div className="schedule-row" key={week.id}>
            <div className="schedule-week-heading" aria-hidden="true" />

            {weekDays.map((day) => {
              const rule = getRule(week.id, day.id);
              const selectedShiftType = rule
                ? getShiftType(rule.shiftTypeId)
                : null;

              return (
                <div
                  key={`${week.id}-${day.id}`}
                  role="button"
                  tabIndex={0}
                  className={`schedule-cell ${rule ? "has-shift" : ""}`}
                  onClick={() => handleCellClick(week.id, day)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      handleCellClick(week.id, day);
                    }
                  }}
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => handleDrop(week.id, day)}
                >
                  {selectedShiftType ? (
                    <span
                      className="shift-note"
                      draggable
                      onClick={(e) => e.stopPropagation()}
                      onDragStart={() =>
                        setDraggedRule({
                          weekInCycle: week.id,
                          dayOfWeek: day.id,
                          dayOfWeekValue: day.value,
                          shiftTypeId: selectedShiftType.id,
                        })
                      }
                      onDragEnd={() => setDraggedRule(null)}
                    >
                      <strong>{selectedShiftType.name}</strong>
                      <small>
                        {formatTime(selectedShiftType.defaultStartTime)}-
                        {formatTime(selectedShiftType.defaultEndTime)}
                      </small>
                      <span
                        className="remove-shift"
                        role="button"
                        tabIndex={0}
                        onClick={(e) => {
                          e.stopPropagation();
                          onChange(week.id, day.value, "");
                        }}
                      >
                        Ta bort
                      </span>
                    </span>
                  ) : (
                    <span className="empty-cell">
                      {selectedShiftTypeId ? "Klicka för att lägga till" : ""}
                    </span>
                  )}
                </div>
              );
            })}
          </div>
        ))}
      </div>
    </section>
  );
}

export default EmployeeBaseSchedule;
