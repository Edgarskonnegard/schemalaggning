function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function ScheduleList({ shifts }) {
  return (
    <section>
      <h2>Skapade passtyper</h2>

      <div className="shift-list">
        {shifts.length === 0 ? (
          <p className="empty-text">Inga passtyper skapade ännu.</p>
        ) : (
          shifts.map((shift) => (
            <div key={shift.id} className="shift-card">
              <h3>{shift.name}</h3>
              <p>Roller: {shift.roleNames.join(", ")}</p>
              <p>
                {formatTime(shift.defaultStartTime)}-
                {formatTime(shift.defaultEndTime)}
              </p>
            </div>
          ))
        )}
      </div>
    </section>
  );
}

export default ScheduleList;
