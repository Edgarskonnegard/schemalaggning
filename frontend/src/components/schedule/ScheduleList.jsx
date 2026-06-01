import ShiftCard from "./ShiftCard";

function ScheduleList({ shifts }) {
  return (
    <section>
      <h2>Skapade passtyper</h2>

      <div className="shift-list">
        {shifts.length === 0 ? (
          <p className="empty-text">Inga passtyper skapade ännu.</p>
        ) : (
          shifts.map((shift) => (
            <ShiftCard key={shift.id} shift={shift} />
          ))
        )}
      </div>
    </section>
  );
}

export default ScheduleList;