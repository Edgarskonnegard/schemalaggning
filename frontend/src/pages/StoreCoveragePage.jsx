import { useEffect, useMemo, useState } from "react";

import {
  deleteStoreCoverageRule,
  getStoreCoverageRules,
  setStoreCoverageRule,
} from "../api/storeCoverageApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import { getStores } from "../api/storesApi";
import ShiftNote from "../components/schedule/ShiftNote";
import Alert from "../components/ui/Alert";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import PageHeader from "../components/ui/PageHeader";
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

const EMPTY_RULE_FORM = {
  shiftTypeId: "",
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

function getDayLabel(dayOfWeek) {
  return DAYS.find((day) => day.value === Number(dayOfWeek))?.label ?? "";
}

function StoreCoveragePage() {
  const [stores, setStores] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [selectedStoreId, setSelectedStoreId] = useState("");
  const [rules, setRules] = useState([]);
  const [ruleForm, setRuleForm] = useState(EMPTY_RULE_FORM);
  const [activeDay, setActiveDay] = useState(null);
  const [editingRule, setEditingRule] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [draggedRule, setDraggedRule] = useState(null);
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

  const rulesByDay = useMemo(() => {
    return rules.reduce((groups, rule) => {
      const key = Number(rule.dayOfWeek);
      groups[key] = groups[key] ?? [];
      groups[key].push(rule);
      return groups;
    }, {});
  }, [rules]);

  function getShiftType(shiftTypeId) {
    return shiftTypes.find((shiftType) => shiftType.id === Number(shiftTypeId));
  }

  function getFormFromShiftType(shiftType) {
    return {
      shiftTypeId: shiftType?.id?.toString() ?? "",
      requiredCount: 1,
      startTime: formatTime(shiftType?.defaultStartTime) || "08:00",
      endTime: formatTime(shiftType?.defaultEndTime) || "16:00",
    };
  }

  function openCreateModal(day) {
    const firstShiftType = shiftTypes[0];
    setActiveDay(day);
    setEditingRule(null);
    setRuleForm(getFormFromShiftType(firstShiftType));
  }

  function openEditModal(rule) {
    setActiveDay({
      value: Number(rule.dayOfWeek),
      label: getDayLabel(rule.dayOfWeek),
    });
    setEditingRule(rule);
    setRuleForm({
      shiftTypeId: rule.shiftTypeId.toString(),
      requiredCount: rule.requiredCount,
      startTime: formatTime(rule.startTime),
      endTime: formatTime(rule.endTime),
    });
  }

  function closeModal() {
    setActiveDay(null);
    setEditingRule(null);
    setRuleForm(EMPTY_RULE_FORM);
  }

  function updateRuleForm(field, value) {
    if (field === "shiftTypeId") {
      const selectedShiftType = getShiftType(value);

      setRuleForm((prev) => ({
        ...prev,
        shiftTypeId: value,
        startTime: formatTime(selectedShiftType?.defaultStartTime) || prev.startTime,
        endTime: formatTime(selectedShiftType?.defaultEndTime) || prev.endTime,
      }));
      return;
    }

    setRuleForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  async function reloadRules() {
    const result = await getStoreCoverageRules(selectedStoreId);
    setRules(result);
  }

  async function handleSaveRule(event) {
    event.preventDefault();

    if (!selectedStoreId || !activeDay || !ruleForm.shiftTypeId) {
      setError("Välj butik, dag och passtyp först.");
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      const nextShiftTypeId = Number(ruleForm.shiftTypeId);

      await setStoreCoverageRule(selectedStoreId, {
        shiftTypeId: nextShiftTypeId,
        dayOfWeek: Number(activeDay.value),
        requiredCount: Number(ruleForm.requiredCount),
        startTime: toApiTime(ruleForm.startTime),
        endTime: toApiTime(ruleForm.endTime),
      });

      if (editingRule && editingRule.shiftTypeId !== nextShiftTypeId) {
        await deleteStoreCoverageRule(selectedStoreId, editingRule.id);
      }

      await reloadRules();
      closeModal();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDrop(targetDay) {
    if (!draggedRule || draggedRule.dayOfWeek === targetDay.value) {
      setDraggedRule(null);
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await setStoreCoverageRule(selectedStoreId, {
        shiftTypeId: draggedRule.shiftTypeId,
        dayOfWeek: targetDay.value,
        requiredCount: draggedRule.requiredCount,
        startTime: draggedRule.startTime,
        endTime: draggedRule.endTime,
      });

      await deleteStoreCoverageRule(selectedStoreId, draggedRule.id);
      await reloadRules();
    } catch (err) {
      setError(err.message);
    } finally {
      setDraggedRule(null);
      setIsSaving(false);
    }
  }

  async function handleDelete(ruleId) {
    setError("");

    try {
      await deleteStoreCoverageRule(selectedStoreId, ruleId);
      setRules((prev) => prev.filter((rule) => rule.id !== ruleId));
      closeModal();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <main className="store-coverage-page">
      <PageHeader
        title="Bemanningsbehov"
        description="Definiera en normalvecka med pass som butiken behöver täcka."
      />

      <Alert>{error}</Alert>

      <section className="coverage-board-card">
        <div className="coverage-board-header">
          <div>
            <h2>Veckomall</h2>
            <p>Klicka på en dag för att skapa pass och dra pass mellan dagar.</p>
          </div>

          {isLoading ? (
            <p className="empty-text">Laddar butiker och passtyper...</p>
          ) : (
            <div className="coverage-store-tool">
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
            </div>
          )}
        </div>

        {!selectedStoreId ? (
          <p className="empty-text">Skapa en butik först.</p>
        ) : (
          <div className="coverage-board">
            {DAYS.map((day) => {
              const dayRules = rulesByDay[day.value] ?? [];

              return (
                <div
                  className="coverage-day-cell"
                  key={day.value}
                  onDragOver={(event) => event.preventDefault()}
                  onDrop={() => handleDrop(day)}
                >
                  <div className="coverage-day-heading">
                    <h3>{day.label}</h3>
                    <Button
                      type="button"
                      disabled={isSaving || shiftTypes.length === 0}
                      onClick={() => openCreateModal(day)}
                    >
                      Skapa pass
                    </Button>
                  </div>

                  {dayRules.length === 0 ? (
                    <p className="coverage-day-empty">Inga pass</p>
                  ) : (
                    <div className="coverage-rule-list">
                      {dayRules.map((rule) => (
                        <ShiftNote
                          draggable
                          key={rule.id}
                          title={rule.shiftTypeName}
                          time={`${rule.requiredCount} st · ${formatTime(
                            rule.startTime
                          )}-${formatTime(rule.endTime)}`}
                          onClick={() => openEditModal(rule)}
                          onDragStart={() => setDraggedRule(rule)}
                          onDragEnd={() => setDraggedRule(null)}
                        >
                          <button
                            className="coverage-rule-remove"
                            type="button"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleDelete(rule.id);
                            }}
                          >
                            Ta bort
                          </button>
                        </ShiftNote>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </section>

      {activeDay && (
        <div className="coverage-modal-backdrop" onClick={closeModal}>
          <section
            className="coverage-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="coverage-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="coverage-modal-header">
              <div>
                <h2 id="coverage-modal-title">
                  {editingRule ? "Ändra pass" : "Skapa pass"}
                </h2>
                <p>{activeDay.label}</p>
              </div>

              <button
                className="coverage-modal-close"
                type="button"
                onClick={closeModal}
              >
                Stäng
              </button>
            </div>

            <form className="coverage-modal-form" onSubmit={handleSaveRule}>
              {error && <p className="coverage-modal-error">{error}</p>}

              <Select
                label="Passtyp"
                value={ruleForm.shiftTypeId}
                onChange={(event) =>
                  updateRuleForm("shiftTypeId", event.target.value)
                }
              >
                <option value="">Välj passtyp</option>
                {shiftTypes.map((shiftType) => (
                  <option key={shiftType.id} value={shiftType.id}>
                    {shiftType.name}
                  </option>
                ))}
              </Select>

              <Input
                label="Antal"
                type="number"
                min="1"
                value={ruleForm.requiredCount}
                onChange={(event) =>
                  updateRuleForm("requiredCount", event.target.value)
                }
              />

              <Input
                label="Start"
                type="time"
                value={ruleForm.startTime}
                onChange={(event) =>
                  updateRuleForm("startTime", event.target.value)
                }
              />

              <Input
                label="Slut"
                type="time"
                value={ruleForm.endTime}
                onChange={(event) => updateRuleForm("endTime", event.target.value)}
              />

              <div className="coverage-modal-actions">
                {editingRule && (
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => handleDelete(editingRule.id)}
                  >
                    Ta bort
                  </Button>
                )}

                <Button type="button" variant="secondary" onClick={closeModal}>
                  Avbryt
                </Button>

                <Button type="submit" disabled={isSaving}>
                  {isSaving ? "Sparar..." : "Spara pass"}
                </Button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  );
}

export default StoreCoveragePage;
