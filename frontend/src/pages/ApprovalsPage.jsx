import { useEffect, useMemo, useState } from "react";

import {
  assignBaseScheduleDraftRule,
  approveBaseScheduleEmployee,
  deleteBaseScheduleDraftRule,
  getBaseScheduleApprovals,
  rejectBaseScheduleEmployee,
} from "../api/approvalsApi";
import Button from "../components/ui/Button";
import Select from "../components/ui/Select";
import "./ApprovalsPage.css";

const DAYS = [
  { value: 1, label: "Mån" },
  { value: 2, label: "Tis" },
  { value: 3, label: "Ons" },
  { value: 4, label: "Tor" },
  { value: 5, label: "Fre" },
  { value: 6, label: "Lör" },
  { value: 0, label: "Sön" },
];

const WEEKS = [1, 2, 3, 4];

function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function formatHours(value) {
  return Number(value).toFixed(1).replace(".0", "");
}

function getPendingItems(approvals) {
  return approvals.flatMap((approval) =>
    approval.employeeApprovals
      .filter((employeeApproval) => employeeApproval.status === "Pending")
      .map((employeeApproval) => ({
        batchId: approval.id,
        storeName: approval.storeName,
        warningCount: approval.warnings.length,
        ...employeeApproval,
      }))
  );
}

function getSelectedKey(item) {
  return item ? `${item.batchId}-${item.employeeId}` : null;
}

function ApprovalsPage() {
  const [approvals, setApprovals] = useState([]);
  const [selectedKey, setSelectedKey] = useState(null);
  const [addTarget, setAddTarget] = useState(null);
  const [selectedUnassignedRuleId, setSelectedUnassignedRuleId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");

  const pendingItems = useMemo(() => getPendingItems(approvals), [approvals]);

  async function loadApprovals(nextSelectedKey = selectedKey) {
    setError("");
    setIsLoading(true);

    try {
      const result = await getBaseScheduleApprovals();
      const nextPendingItems = getPendingItems(result);
      const selectedExists = nextPendingItems.some(
        (item) => getSelectedKey(item) === nextSelectedKey
      );

      setApprovals(result);
      setSelectedKey(
        selectedExists
          ? nextSelectedKey
          : getSelectedKey(nextPendingItems[0]) ?? null
      );
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadApprovals(null);
  }, []);

  const selectedItem = pendingItems.find(
    (item) => getSelectedKey(item) === selectedKey
  );

  const selectedApproval = approvals.find(
    (approval) => approval.id === selectedItem?.batchId
  );

  const employeeRules = useMemo(() => {
    if (!selectedApproval || !selectedItem) {
      return [];
    }

    return selectedApproval.draftRules.filter(
      (rule) => rule.employeeId === selectedItem.employeeId
    );
  }, [selectedApproval, selectedItem]);

  const employeeSummary = selectedApproval?.employeeSummaries.find(
    (summary) => summary.employeeId === selectedItem?.employeeId
  );

  const rulesByWeekAndDay = useMemo(() => {
    return employeeRules.reduce((groups, rule) => {
      const key = `${rule.weekInCycle}-${rule.dayOfWeek}`;
      groups[key] = groups[key] ?? [];
      groups[key].push(rule);
      return groups;
    }, {});
  }, [employeeRules]);

  const unassignedByWeekAndDay = useMemo(() => {
    if (!selectedApproval) {
      return {};
    }

    return selectedApproval.unassignedDraftRules.reduce((groups, rule) => {
      const key = `${rule.weekInCycle}-${rule.dayOfWeek}`;
      groups[key] = groups[key] ?? [];
      groups[key].push(rule);
      return groups;
    }, {});
  }, [selectedApproval]);

  async function handleDecision(action) {
    if (!selectedItem) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      if (action === "approve") {
        await approveBaseScheduleEmployee(
          selectedItem.batchId,
          selectedItem.employeeId
        );
      } else {
        await rejectBaseScheduleEmployee(
          selectedItem.batchId,
          selectedItem.employeeId
        );
      }

      window.dispatchEvent(new Event("approvals-updated"));
      await loadApprovals(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  function openAddModal(weekInCycle, dayOfWeek, unassignedRules) {
    setError("");
    setAddTarget({ weekInCycle, dayOfWeek, unassignedRules });
    setSelectedUnassignedRuleId(unassignedRules[0]?.id?.toString() ?? "");
  }

  function closeAddModal() {
    setAddTarget(null);
  }

  async function handleAddDraftRule(event) {
    event.preventDefault();

    if (!selectedItem || !addTarget || !selectedUnassignedRuleId) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await assignBaseScheduleDraftRule(
        selectedItem.batchId,
        selectedItem.employeeId,
        Number(selectedUnassignedRuleId)
      );

      await loadApprovals(selectedKey);
      closeAddModal();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeleteDraftRule(ruleId) {
    if (!selectedItem) {
      return;
    }

    setError("");
    setIsSaving(true);

    try {
      await deleteBaseScheduleDraftRule(
        selectedItem.batchId,
        selectedItem.employeeId,
        ruleId
      );
      await loadApprovals(selectedKey);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="approvals-page">
      <div className="page-header">
        <h1>Godkännanden</h1>
        <p>Granska en anställds genererade grundschema åt gången.</p>
      </div>

      {error && <p className="page-error">{error}</p>}

      <div className="approvals-layout">
        <aside className="approvals-list">
          <h2>Väntar</h2>

          {isLoading ? (
            <p className="empty-text">Laddar godkännanden...</p>
          ) : pendingItems.length === 0 ? (
            <p className="empty-text">Inget väntar på godkännande.</p>
          ) : (
            pendingItems.map((item) => (
              <button
                className={`approval-list-item ${
                  getSelectedKey(item) === selectedKey ? "active" : ""
                }`}
                key={getSelectedKey(item)}
                type="button"
                onClick={() => setSelectedKey(getSelectedKey(item))}
              >
                <strong>{item.employeeName}</strong>
                <span>{item.storeName}</span>
                {item.warningCount > 0 && (
                  <span>{item.warningCount} varning(ar)</span>
                )}
              </button>
            ))
          )}
        </aside>

        <section className="approval-detail">
          {!selectedItem || !selectedApproval ? (
            <p className="empty-text">Välj ett godkännande att granska.</p>
          ) : (
            <>
              <div className="approval-detail-header">
                <div>
                  <h2>{selectedItem.employeeName}</h2>
                  <p>
                    {selectedApproval.storeName} ·{" "}
                    {employeeRules.length} regler
                  </p>
                </div>

                <div className="approval-actions">
                  <Button
                    type="button"
                    variant="secondary"
                    disabled={isSaving}
                    onClick={() => handleDecision("reject")}
                  >
                    Avvisa
                  </Button>
                  <Button
                    type="button"
                    disabled={isSaving}
                    onClick={() => handleDecision("approve")}
                  >
                    {isSaving ? "Sparar..." : "Godkänn"}
                  </Button>
                </div>
              </div>

              {selectedApproval.warnings.length > 0 && (
                <div className="approval-warnings">
                  {selectedApproval.warnings.map((warning) => (
                    <p key={warning}>{warning}</p>
                  ))}
                </div>
              )}

              {selectedApproval.unassignedDraftRules.length > 0 && (
                <section className="unassigned-summary">
                  <h3>Oplacerade pass i förslaget</h3>
                  <div className="unassigned-list">
                    {selectedApproval.unassignedDraftRules.map((rule) => (
                      <span key={rule.id}>
                        V{rule.weekInCycle} · {rule.shiftTypeName}
                      </span>
                    ))}
                  </div>
                </section>
              )}

              {employeeSummary && (
                <section className="approval-summary">
                  <h3>Timmar per vecka</h3>

                  <div className="summary-table">
                    <div className="summary-row summary-heading employee-summary-row">
                      <span>Mål</span>
                      <span>V1</span>
                      <span>V2</span>
                      <span>V3</span>
                      <span>V4</span>
                    </div>

                    <div className="summary-row employee-summary-row">
                      <span>{formatHours(employeeSummary.targetWeeklyHours)}h</span>
                      {employeeSummary.weeklyHours.map((hours, index) => (
                        <span key={index}>{formatHours(hours)}h</span>
                      ))}
                    </div>
                  </div>
                </section>
              )}

              <section className="approval-board">
                <div className="approval-corner" />

                {DAYS.map((day) => (
                  <div className="approval-day-heading" key={day.value}>
                    {day.label}
                  </div>
                ))}

                {WEEKS.map((week) => (
                  <div className="approval-row" key={week}>
                    <div className="approval-week-heading">V{week}</div>

                    {DAYS.map((day) => {
                      const rules = rulesByWeekAndDay[`${week}-${day.value}`] ?? [];
                      const unassignedRules =
                        unassignedByWeekAndDay[`${week}-${day.value}`] ?? [];

                      return (
                        <div className="approval-cell" key={`${week}-${day.value}`}>
                          {rules.map((rule) => (
                            <article className="approval-shift-note" key={rule.id}>
                              <div>
                                <strong>{rule.shiftTypeName}</strong>
                                <small>
                                  {formatTime(rule.startTime)}-{formatTime(rule.endTime)}
                                </small>
                              </div>

                              <button
                                className="approval-shift-remove"
                                type="button"
                                disabled={isSaving}
                                onClick={() => handleDeleteDraftRule(rule.id)}
                              >
                                Ta bort
                              </button>
                            </article>
                          ))}

                          {rules.length === 0 && unassignedRules.length > 0 && (
                            <button
                              className="approval-add-shift"
                              type="button"
                              disabled={isSaving}
                              onClick={() =>
                                openAddModal(week, day.value, unassignedRules)
                              }
                            >
                              Lägg till ({unassignedRules.length})
                            </button>
                          )}
                        </div>
                      );
                    })}
                  </div>
                ))}
              </section>
            </>
          )}
        </section>
      </div>

      {addTarget && (
        <div className="approval-modal-backdrop" onClick={closeAddModal}>
          <section
            className="approval-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="approval-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="approval-modal-header">
              <div>
                <h2 id="approval-modal-title">Lägg till pass</h2>
                <p>
                  {selectedItem?.employeeName} · vecka {addTarget.weekInCycle}
                </p>
              </div>

              <button
                className="approval-modal-close"
                type="button"
                onClick={closeAddModal}
              >
                Stäng
              </button>
            </div>

            <form className="approval-modal-form" onSubmit={handleAddDraftRule}>
              {error && <p className="approval-modal-error">{error}</p>}

              <Select
                label="Oplacerat pass"
                value={selectedUnassignedRuleId}
                onChange={(event) =>
                  setSelectedUnassignedRuleId(event.target.value)
                }
              >
                {addTarget.unassignedRules.map((rule) => (
                  <option key={rule.id} value={rule.id}>
                    {rule.shiftTypeName} · {formatTime(rule.startTime)}-
                    {formatTime(rule.endTime)}
                  </option>
                ))}
              </Select>

              <div className="approval-modal-actions">
                <Button type="button" variant="secondary" onClick={closeAddModal}>
                  Avbryt
                </Button>
                <Button
                  type="submit"
                  disabled={isSaving || !selectedUnassignedRuleId}
                >
                  {isSaving ? "Sparar..." : "Lägg till"}
                </Button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  );
}

export default ApprovalsPage;
