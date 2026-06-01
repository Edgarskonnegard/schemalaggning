import "./EmployeeBaseSchedule.css";

const weekDays = [
  { id: "monday", label: "Mån", fullLabel: "Måndag" },
  { id: "tuesday", label: "Tis", fullLabel: "Tisdag" },
  { id: "wednesday", label: "Ons", fullLabel: "Onsdag" },
  { id: "thursday", label: "Tor", fullLabel: "Torsdag" },
  { id: "friday", label: "Fre", fullLabel: "Fredag" },
  { id: "saturday", label: "Lör", fullLabel: "Lördag" },
  { id: "sunday", label: "Sön", fullLabel: "Söndag" },
];

function EmployeeBaseSchedule({
  baseSchedule,
  shiftTypes,
  onChange,
}) {
  function getRuleForDay(dayId) {
    return baseSchedule.find((rule) => rule.day === dayId);
  }

  function getShiftType(shiftTypeId) {
    return shiftTypes.find(
      (shiftType) => shiftType.id === shiftTypeId
    );
  }

  return (
    <section className="base-schedule-card">
      <div className="base-schedule-header">
        <div>
          <h2>Grundschema</h2>
          <p>
            Välj vilka grundpass den anställda normalt arbetar
            under veckan.
          </p>
        </div>
      </div>

      <div className="base-schedule-grid">
        {weekDays.map((day) => {
          const rule = getRuleForDay(day.id);
          const selectedShiftType = rule
            ? getShiftType(rule.shiftTypeId)
            : null;

          return (
            <div key={day.id} className="base-schedule-day">
              <div className="day-header">
                <span>{day.label}</span>
                <strong>{day.fullLabel}</strong>
              </div>

              <div className="form-group">
                <label>Grundpass</label>

                <select
                  value={selectedShiftType?.id || ""}
                  onChange={(e) =>
                    onChange(day.id, e.target.value)
                  }
                >
                  <option value="">Ledig</option>

                  {shiftTypes.map((shiftType) => (
                    <option
                      key={shiftType.id}
                      value={shiftType.id}
                    >
                      {shiftType.name}
                    </option>
                  ))}
                </select>
              </div>

              {selectedShiftType ? (
                <div className="selected-shift-preview">
                  <strong>{selectedShiftType.name}</strong>
                  <span>
                    {selectedShiftType.defaultStartTime}-
                    {selectedShiftType.defaultEndTime}
                  </span>
                </div>
              ) : (
                <div className="empty-day-preview">
                  Ingen återkommande tid
                </div>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}

export default EmployeeBaseSchedule;