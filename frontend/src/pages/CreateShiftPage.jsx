import { useEffect, useState } from "react";

import { getRoles } from "../api/rolesApi";
import {
  createShiftType,
  getShiftTypes,
  updateShiftType,
} from "../api/shiftTypesApi";
import CreateShiftForm from "../components/schedule/CreateShiftForm";
import ScheduleList from "../components/schedule/ScheduleList";
import Alert from "../components/ui/Alert";
import PageHeader from "../components/ui/PageHeader";
import "./CreateShiftPage.css";

function CreateShiftPage() {
  const [roles, setRoles] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [rolesResult, shiftTypesResult] = await Promise.all([
        getRoles(),
        getShiftTypes(),
      ]);

      setRoles(rolesResult);
      setShiftTypes(shiftTypesResult);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  async function handleCreateShiftType(newShiftType) {
    try {
      setError("");
      const created = await createShiftType(newShiftType);
      setShiftTypes((prev) =>
        [...prev, created].sort((a, b) => a.name.localeCompare(b.name))
      );
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleUpdateShiftType(id, shiftType) {
    try {
      setError("");
      await updateShiftType(id, shiftType);
      await loadData();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <main className="create-shift-page">
      <PageHeader
        title="Passtyper"
        description="Skapa passmallar som används senare vid schemagenerering och schemaredigering."
      />

      <Alert>{error}</Alert>

      <div className="create-shift-layout">
        <section className="left-panel">
          <CreateShiftForm
            roles={roles}
            onCreateShiftType={handleCreateShiftType}
          />
        </section>

        <section className="right-panel">
          {isLoading ? (
            <p className="empty-text">Laddar passtyper...</p>
          ) : (
            <ScheduleList
              roles={roles}
              shifts={shiftTypes}
              onUpdateShiftType={handleUpdateShiftType}
            />
          )}
        </section>
      </div>
    </main>
  );
}

export default CreateShiftPage;
