import { useState } from "react";

const weekDays = [
  { id: "monday", label: "Mån" },
  { id: "tuesday", label: "Tis" },
  { id: "wednesday", label: "Ons" },
  { id: "thursday", label: "Tor" },
  { id: "friday", label: "Fre" },
  { id: "saturday", label: "Lör" },
  { id: "sunday", label: "Sön" },
];

function CreateShiftForm({ onCreateShift }) {
  const [name, setName] = useState("");
  const [role, setRole] = useState("");

  const [dayRules, setDayRules] = useState([
    {
      id: Date.now(),
      days: [],
      startTime: "",
      endTime: "",
    },
  ]);

  function handleDayToggle(ruleId, dayId) {
    setDayRules((prev) =>
      prev.map((rule) => {
        // aktiv regel
        if (rule.id === ruleId) {
          const exists = rule.days.includes(dayId);

          return {
            ...rule,
            days: exists
              ? rule.days.filter((day) => day !== dayId)
              : [...rule.days, dayId],
          };
        }

        // ta bort dagen från andra regler
        return {
          ...rule,
          days: rule.days.filter((day) => day !== dayId),
        };
      })
    );
  }

  function handleRuleChange(ruleId, field, value) {
    setDayRules((prev) =>
      prev.map((rule) =>
        rule.id === ruleId
          ? {
              ...rule,
              [field]: value,
            }
          : rule
      )
    );
  }

  function addDayRule() {
    setDayRules((prev) => [
      ...prev,
      {
        id: Date.now(),
        days: [],
        startTime: "",
        endTime: "",
      },
    ]);
  }

  function removeDayRule(ruleId) {
    setDayRules((prev) =>
      prev.filter((rule) => rule.id !== ruleId)
    );
  }

  function handleSubmit(e) {
    e.preventDefault();

    const validRules = dayRules.filter(
      (rule) =>
        rule.days.length > 0 &&
        rule.startTime &&
        rule.endTime
    );

    if (
      !name.trim() ||
      !role.trim() ||
      validRules.length === 0
    ) {
      return;
    }

    const newShift = {
      id: Date.now(),
      name,
      role,
      dayRules: validRules,
    };

    onCreateShift(newShift);

    setName("");
    setRole("");

    setDayRules([
      {
        id: Date.now(),
        days: [],
        startTime: "",
        endTime: "",
      },
    ]);
  }

  return (
    <section className="shift-form-card">
      <h2>Skapa passtyp</h2>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Namn</label>

          <input
            value={name}
            onChange={(e) =>
              setName(e.target.value)
            }
            placeholder="Ex. Öppning"
          />
        </div>

        <div className="form-group">
          <label>Roll</label>

          <input
            value={role}
            onChange={(e) =>
              setRole(e.target.value)
            }
            placeholder="Ex. Butik"
          />
        </div>

        <h3>Dagsregler</h3>

        {dayRules.map((rule) => (
          <div
            key={rule.id}
            className="day-rule-card"
          >
            <div className="day-buttons">
              {weekDays.map((day) => (
                <button
                  key={day.id}
                  type="button"
                  className={
                    rule.days.includes(day.id)
                      ? "active"
                      : ""
                  }
                  onClick={() =>
                    handleDayToggle(
                      rule.id,
                      day.id
                    )
                  }
                >
                  {day.label}
                </button>
              ))}
            </div>

            <div className="time-row">
              <div className="form-group">
                <label>Start</label>

                <input
                  type="time"
                  value={rule.startTime}
                  onChange={(e) =>
                    handleRuleChange(
                      rule.id,
                      "startTime",
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-group">
                <label>Slut</label>

                <input
                  type="time"
                  value={rule.endTime}
                  onChange={(e) =>
                    handleRuleChange(
                      rule.id,
                      "endTime",
                      e.target.value
                    )
                  }
                />
              </div>
            </div>

            {dayRules.length > 1 && (
              <button
                type="button"
                className="remove-rule-btn"
                onClick={() =>
                  removeDayRule(rule.id)
                }
              >
                Ta bort regel
              </button>
            )}
          </div>
        ))}

        <button
          type="button"
          className="secondary-btn"
          onClick={addDayRule}
        >
          Lägg till dagsregel
        </button>

        <button
          type="submit"
          className="primary-btn"
        >
          Skapa passtyp
        </button>
      </form>
    </section>
  );
}

export default CreateShiftForm;