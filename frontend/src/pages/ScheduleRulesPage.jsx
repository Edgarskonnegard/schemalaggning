import { useEffect, useState } from "react";

import {
  getScheduleGenerationSettings,
  updateScheduleGenerationSettings,
} from "../api/scheduleGenerationSettingsApi";
import { getStores } from "../api/storesApi";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import Select from "../components/ui/Select";
import "./ScheduleRulesPage.css";

function ScheduleRulesPage() {
  const [stores, setStores] = useState([]);
  const [selectedStoreId, setSelectedStoreId] = useState("");
  const [form, setForm] = useState({
    minimumRestHours: 11,
    maxConsecutiveWorkDays: 5,
    balanceWeekends: true,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadStores() {
      setError("");
      setIsLoading(true);

      try {
        const storesResult = await getStores();
        setStores(storesResult);
        setSelectedStoreId(storesResult[0]?.id?.toString() ?? "");
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
      }
    }

    loadStores();
  }, []);

  useEffect(() => {
    async function loadSettings() {
      if (!selectedStoreId) {
        return;
      }

      setError("");
      setStatusMessage("");

      try {
        const settings = await getScheduleGenerationSettings(selectedStoreId);
        setForm({
          minimumRestHours: settings.minimumRestHours,
          maxConsecutiveWorkDays: settings.maxConsecutiveWorkDays,
          balanceWeekends: settings.balanceWeekends,
        });
      } catch (err) {
        setError(err.message);
      }
    }

    loadSettings();
  }, [selectedStoreId]);

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!selectedStoreId) {
      setError("Välj butik först.");
      return;
    }

    setError("");
    setStatusMessage("");
    setIsSaving(true);

    try {
      const updatedSettings = await updateScheduleGenerationSettings(
        selectedStoreId,
        {
          minimumRestHours: Number(form.minimumRestHours),
          maxConsecutiveWorkDays: Number(form.maxConsecutiveWorkDays),
          balanceWeekends: Boolean(form.balanceWeekends),
        }
      );

      setForm({
        minimumRestHours: updatedSettings.minimumRestHours,
        maxConsecutiveWorkDays: updatedSettings.maxConsecutiveWorkDays,
        balanceWeekends: updatedSettings.balanceWeekends,
      });
      setStatusMessage("Reglerna har sparats.");
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="schedule-rules-page">
      <div className="page-header">
        <h1>Schemagenereringsregler</h1>
        <p>Styr vilka regler som ska gälla när grundscheman genereras.</p>
      </div>

      {error && <p className="page-error">{error}</p>}
      {statusMessage && <p className="page-status">{statusMessage}</p>}

      <section className="schedule-rules-card">
        <form className="schedule-rules-form" onSubmit={handleSubmit}>
          <Select
            label="Butik"
            value={selectedStoreId}
            onChange={(event) => setSelectedStoreId(event.target.value)}
          >
            <option value="">Välj butik</option>
            {stores.map((store) => (
              <option key={store.id} value={store.id}>
                {store.name}
              </option>
            ))}
          </Select>

          <Input
            label="Minsta dygnsvila (timmar)"
            min="0"
            max="24"
            step="0.25"
            type="number"
            value={form.minimumRestHours}
            onChange={(event) =>
              updateForm("minimumRestHours", event.target.value)
            }
          />

          <Input
            label="Max arbetsdagar i rad"
            min="1"
            max="28"
            step="1"
            type="number"
            value={form.maxConsecutiveWorkDays}
            onChange={(event) =>
              updateForm("maxConsecutiveWorkDays", event.target.value)
            }
          />

          <label className="schedule-rules-checkbox">
            <input
              type="checkbox"
              checked={form.balanceWeekends}
              onChange={(event) =>
                updateForm("balanceWeekends", event.target.checked)
              }
            />
            <span>Ledig minst varannan helg</span>
          </label>

          <Button type="submit" disabled={isLoading || isSaving}>
            {isSaving ? "Sparar..." : "Spara regler"}
          </Button>
        </form>

        <div className="schedule-rule-note">
          <h2>Regler vid generering</h2>
          <p>
            Dygnsvila och max arbetsdagar i rad används som hårda regler.
            När varannan helg är aktiverad försöker systemet undvika att samma
            anställd jobbar två helger i rad.
          </p>
        </div>
      </section>
    </main>
  );
}

export default ScheduleRulesPage;
