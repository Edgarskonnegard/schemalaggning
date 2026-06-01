import { useState } from "react";
import CreateShiftForm from "../components/schedule/CreateShiftForm";
import ScheduleList from "../components/schedule/ScheduleList";
import "./CreateShiftPage.css";

function CreateShiftPage() {
  const [shifts, setShifts] = useState([]);

  function handleCreateShift(newShift) {
    setShifts((prev) => [...prev, newShift]);
  }

  return (
    <main className="create-shift-page">
      <div className="page-header">
        <h1>Passtyper</h1>

        <p>
          Skapa passmallar som används senare vid
          schemagenerering och schemaredigering.
        </p>
      </div>

      <div className="create-shift-layout">

        <section className="left-panel">
          <CreateShiftForm
            onCreateShift={handleCreateShift}
          />
        </section>

        <section className="right-panel">
          <ScheduleList
            shifts={shifts}
          />
        </section>

      </div>
    </main>
  );
}

export default CreateShiftPage;