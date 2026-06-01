import Calendar from "../components/calendar/Calendar";

function SchedulePage() {
  return (
    <main>
      <h1>Schema</h1>

      <p>
        Här visas schemat för vald månad.
      </p>

      <Calendar />
    </main>
  );
}

export default SchedulePage;