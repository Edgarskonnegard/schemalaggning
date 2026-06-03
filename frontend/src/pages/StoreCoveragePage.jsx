import { useEffect, useMemo, useState } from "react";

import {
  deleteStoreCoverageRule,
  getStoreCoverageRules,
  setStoreCoverageRule,
} from "../api/storeCoverageApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import { getStores } from "../api/storesApi";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import Select from "../components/ui/Select";
import "./StoreCoveragePage.css";

const DAYS = [
  { value: 1, label: "Måndag" },
  { value: 2, label: "Tisdag" },
  { value: 3, label: "Onsdag" },
  { value: 4, label: "Torsdag" },
  { value: 5, label: "Fredag" },
  { value: 6, label: "Lördag" },
  { value: 0, label: "Söndag" },
];

const EMPTY_FORM = {
  shiftTypeId: "",
  weekInCycle: 1,
  dayOfWeek: 1,
  requiredCount: 1,
  startTime: "08:00",
  endTime: "16:00",
};

function formatTime(value) {
  if (!value) return "";
  return value.slice(0, 5);
}

function toApiTime(value) {
  return value.length === 5 ? `${value}:00` : value;
}

function getDayName(dayOfWeek) {
  return DAYS.find((day) => day.value === Number(dayOfWeek))?.label ?? "";
}

function StoreCoveragePage() {
  const [stores, setStores] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [selectedStoreId, setSelectedStoreId] = useState("");
  const [rules, setRules] = useState([]);
  const [form, setForm] = useState(EMPTY_FORM);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadBaseData() {
      setError("");
      setIsLoading(true);

      try {
        const [storesResult, shiftTypesResult] = await Promise.all([
          getStores(),
          getShiftTypes(),
        ]);

        setStores(storesResult);
        setShiftTypes(shiftTypesResult);
        setSelectedStoreId(storesResult[0]?.id?.toString() ?? "");
        setForm((prev) => ({
          ...prev,
          shiftTypeId: shiftTypesResult[0]?.id?.toString() ?? "",
        }));
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
      }
    }

    loadBaseData();
  }, []);

  useEffect(() => {
    async function loadRules() {
      if (!selectedStoreId) {
        setRules([]);
        return;
      }

      setError("");

      try {
        const result = await getStoreCoverageRules(selectedStoreId);
        setRules(result);
      } catch (err) {
        setError(err.message);
      }
    }

    loadRules();
  }, [selectedStoreId]);

  const groupedRules = useMemo(() => {
    return rules.reduce((groups, rule) => {
      const key = `Vecka ${rule.weekInCycle}`;
      groups[key] = groups[key] ?? [];
      groups[key].push(rule);
      return groups;
    }, {});
  }, [rules]);

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!selectedStoreId || !form.shiftTypeId) {
      setError("Välj butik och passtyp först.");
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await setStoreCoverageRule(selectedStoreId, {
        shiftTypeId: Number(form.shiftTypeId),
        weekInCycle: Number(form.weekInCycle),
        dayOfWeek: Number(form.dayOfWeek),
        requiredCount: Number(form.requiredCount),
        startTime: toApiTime(form.startTime),
        endTime: toApiTime(form.endTime),
      });

      const result = await getStoreCoverageRules(selectedStoreId);
      setRules(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(ruleId) {
    setError("");

    try {
      await deleteStoreCoverageRule(selectedStoreId, ruleId);
      setRules((prev) => prev.filter((rule) => rule.id !== ruleId));
    } catch (err) {
      setError(err.message);
    }
  }

  function handleEdit(rule) {
    setForm({
      shiftTypeId: rule.shiftTypeId.toString(),
      weekInCycle: rule.weekInCycle,
      dayOfWeek: rule.dayOfWeek,
      requiredCount: rule.requiredCount,
      startTime: formatTime(rule.startTime),
      endTime: formatTime(rule.endTime),
    });
  }

  return (
    <main className="store-coverage-page">
      <div className="page-header">
        <h1>Bemanningsbehov</h1>
        <p>
          Definiera vilka pass butiken behöver täcka i fyraveckorscykeln.
        </p>
      </div>

      {error && <p className="page-error">{error}</p>}

      <div className="coverage-layout">
        <section className="coverage-editor">
          <h2>Lägg in behov</h2>

          {isLoading ? (
            <p className="empty-text">Laddar butiker och passtyper...</p>
          ) : (
            <form onSubmit={handleSubmit} className="coverage-form">
              <Select
                label="Butik"
                value={selectedStoreId}
                onChange={(event) => setSelectedStoreId(event.target.value)}
              >
                {stores.map((store) => (
                  <option key={store.id} value={store.id}>
                    {store.name}
                  </option>
                ))}
              </Select>

              <Select
                label="Passtyp"
                value={form.shiftTypeId}
                onChange={(event) =>
                  updateForm("shiftTypeId", event.target.value)
                }
              >
                {shiftTypes.map((shiftType) => (
                  <option key={shiftType.id} value={shiftType.id}>
                    {shiftType.name}
                  </option>
                ))}
              </Select>

              <div className="coverage-form-grid">
                <Select
                  label="Vecka"
                  value={form.weekInCycle}
                  onChange={(event) =>
                    updateForm("weekInCycle", event.target.value)
                  }
                >
                  {[1, 2, 3, 4].map((week) => (
                    <option key={week} value={week}>
                      {week}
                    </option>
                  ))}
                </Select>

                <Select
                  label="Dag"
                  value={form.dayOfWeek}
                  onChange={(event) =>
                    updateForm("dayOfWeek", event.target.value)
                  }
                >
                  {DAYS.map((day) => (
                    <option key={day.value} value={day.value}>
                      {day.label}
                    </option>
                  ))}
                </Select>

                <Input
                  label="Antal"
                  type="number"
                  min="1"
                  value={form.requiredCount}
                  onChange={(event) =>
                    updateForm("requiredCount", event.target.value)
                  }
                />

                <Input
                  label="Start"
                  type="time"
                  value={form.startTime}
                  onChange={(event) =>
                    updateForm("startTime", event.target.value)
                  }
                />

                <Input
                  label="Slut"
                  type="time"
                  value={form.endTime}
                  onChange={(event) => updateForm("endTime", event.target.value)}
                />
              </div>

              <Button type="submit" disabled={isSaving}>
                {isSaving ? "Sparar..." : "Spara behov"}
              </Button>
            </form>
          )}
        </section>

        <section className="coverage-list">
          <h2>Nuvarande behov</h2>

          {!selectedStoreId ? (
            <p className="empty-text">Skapa en butik först.</p>
          ) : rules.length === 0 ? (
            <p className="empty-text">Inga behov är inlagda ännu.</p>
          ) : (
            Object.entries(groupedRules).map(([week, weekRules]) => (
              <div className="coverage-week" key={week}>
                <h3>{week}</h3>

                <div className="coverage-rule-list">
                  {weekRules.map((rule) => (
                    <article className="coverage-rule" key={rule.id}>
                      <div>
                        <strong>
                          {getDayName(rule.dayOfWeek)} · {rule.shiftTypeName}
                        </strong>
                        <span>
                          {rule.requiredCount} st · {formatTime(rule.startTime)}
                          {"-"}
                          {formatTime(rule.endTime)}
                        </span>
                      </div>

                      <div className="coverage-rule-actions">
                        <Button type="button" onClick={() => handleEdit(rule)}>
                          Ändra
                        </Button>
                        <Button
                          type="button"
                          variant="secondary"
                          onClick={() => handleDelete(rule.id)}
                        >
                          Ta bort
                        </Button>
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            ))
          )}
        </section>
      </div>
    </main>
  );
}

export default StoreCoveragePage;
