const dayLabels = {
  monday: "Mån",
  tuesday: "Tis",
  wednesday: "Ons",
  thursday: "Tor",
  friday: "Fre",
  saturday: "Lör",
  sunday: "Sön",
};

function ShiftCard({ shift }) {
  return (
    <article className="shift-card">
      <div className="shift-card-header">
        <h3>{shift.name}</h3>
        <span>{shift.role}</span>
      </div>

      <div className="rule-list">
        {shift.dayRules.map((rule) => (
          <div key={rule.id} className="rule-row">
            <strong>
              {rule.days.map((day) => dayLabels[day]).join(", ")}
            </strong>

            <span>
              {rule.startTime} - {rule.endTime}
            </span>
          </div>
        ))}
      </div>
    </article>
  );
}

export default ShiftCard;