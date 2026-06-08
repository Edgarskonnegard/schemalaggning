import { useEffect, useMemo, useState } from "react";

import { getMe } from "../api/meApi";
import { createShiftComment } from "../api/shiftCommentsApi";
import {
  approveShiftSwap,
  createShiftSwapRequest,
  getMyShiftSwaps,
  getShiftSwapCandidates,
  rejectShiftSwap,
} from "../api/shiftSwapsApi";
import Alert from "../components/ui/Alert";
import Badge from "../components/ui/Badge";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import "./MySchedulePage.css";

function formatDate(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function MySchedulePage() {
  const [me, setMe] = useState(null);
  const [shiftSwaps, setShiftSwaps] = useState([]);
  const [selectedShift, setSelectedShift] = useState(null);
  const [swapShift, setSwapShift] = useState(null);
  const [swapCandidates, setSwapCandidates] = useState([]);
  const [selectedSwapEmployeeId, setSelectedSwapEmployeeId] = useState("");
  const [commentMessage, setCommentMessage] = useState("");
  const [swapMessage, setSwapMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSwapCandidates, setIsLoadingSwapCandidates] = useState(false);
  const [isSavingComment, setIsSavingComment] = useState(false);
  const [isSavingSwap, setIsSavingSwap] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [error, setError] = useState("");

  async function loadSchedule() {
    setError("");
    setIsLoading(true);

    try {
      const [meResult, swapResult] = await Promise.all([
        getMe(),
        getMyShiftSwaps(),
      ]);
      setMe(meResult);
      setShiftSwaps(swapResult);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadSchedule();
  }, []);

  const groupedShifts = useMemo(() => {
    if (!me?.upcomingShifts) {
      return [];
    }

    const groups = me.upcomingShifts.reduce((map, shift) => {
      map.set(shift.date, [...(map.get(shift.date) || []), shift]);
      return map;
    }, new Map());

    return [...groups.entries()];
  }, [me]);

  const receivedSwapRequests = useMemo(() => {
    if (!me?.employee) {
      return [];
    }

    return shiftSwaps.filter(
      (request) => request.toEmployeeId === me.employee.id
    );
  }, [me, shiftSwaps]);

  const sentSwapRequests = useMemo(() => {
    if (!me?.employee) {
      return [];
    }

    return shiftSwaps.filter(
      (request) => request.fromEmployeeId === me.employee.id
    );
  }, [me, shiftSwaps]);

  function openCommentModal(shift) {
    setError("");
    setStatusMessage("");
    setSelectedShift(shift);
    setCommentMessage("");
  }

  function closeCommentModal() {
    if (isSavingComment) {
      return;
    }

    setSelectedShift(null);
    setCommentMessage("");
  }

  async function handleCommentSubmit(event) {
    event.preventDefault();

    if (!selectedShift) {
      return;
    }

    setError("");
    setStatusMessage("");
    setIsSavingComment(true);

    try {
      await createShiftComment(selectedShift.id, commentMessage);
      setStatusMessage("Kommentaren är skickad till admin.");
      setSelectedShift(null);
      setCommentMessage("");
      window.dispatchEvent(new Event("approvals-updated"));
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSavingComment(false);
    }
  }

  async function openSwapModal(shift) {
    setError("");
    setStatusMessage("");
    setSwapShift(shift);
    setSwapCandidates([]);
    setSelectedSwapEmployeeId("");
    setSwapMessage("");
    setIsLoadingSwapCandidates(true);

    try {
      const candidates = await getShiftSwapCandidates(shift.id);
      setSwapCandidates(candidates);
      setSelectedSwapEmployeeId(candidates[0]?.employeeId?.toString() ?? "");
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoadingSwapCandidates(false);
    }
  }

  function closeSwapModal() {
    if (isSavingSwap) {
      return;
    }

    setSwapShift(null);
    setSwapCandidates([]);
    setSelectedSwapEmployeeId("");
    setSwapMessage("");
  }

  async function handleSwapSubmit(event) {
    event.preventDefault();

    if (!swapShift || !selectedSwapEmployeeId) {
      return;
    }

    setError("");
    setStatusMessage("");
    setIsSavingSwap(true);

    try {
      await createShiftSwapRequest(swapShift.id, {
        toEmployeeId: Number(selectedSwapEmployeeId),
        message: swapMessage,
      });
      setStatusMessage("Bytesförfrågan är skickad.");
      setSwapShift(null);
      setSwapCandidates([]);
      setSelectedSwapEmployeeId("");
      setSwapMessage("");
      setShiftSwaps(await getMyShiftSwaps());
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSavingSwap(false);
    }
  }

  async function handleSwapDecision(id, action) {
    setError("");
    setStatusMessage("");
    setIsSavingSwap(true);

    try {
      if (action === "approve") {
        await approveShiftSwap(id);
        setStatusMessage("Passbytet är godkänt och passet har flyttats.");
      } else {
        await rejectShiftSwap(id);
        setStatusMessage("Passbytet är nekat.");
      }

      await loadSchedule();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSavingSwap(false);
    }
  }

  return (
    <main className="my-schedule-page">
      <PageHeader
        title="Mitt schema"
        description="Dina kommande publicerade pass."
      />

      <Alert>{error}</Alert>
      <Alert variant="success">{statusMessage}</Alert>

      {isLoading ? (
        <Card>
          <p className="my-schedule-muted">Laddar ditt schema...</p>
        </Card>
      ) : !me?.employee ? (
        <Card>
          <p className="my-schedule-muted">
            Det här kontot är inte kopplat till en anställd profil.
          </p>
        </Card>
      ) : (
        <>
          {(receivedSwapRequests.length > 0 || sentSwapRequests.length > 0) && (
            <Card className="my-schedule-swaps">
              <h2>Passbyten</h2>

              {receivedSwapRequests.length > 0 && (
                <section className="my-schedule-swap-section">
                  <h3>Mottagna förfrågningar</h3>
                  {receivedSwapRequests.map((request) => (
                    <article className="my-schedule-swap-item" key={request.id}>
                      <div>
                        <strong>{request.fromEmployeeName}</strong>
                        <span>
                          vill ge dig {request.shiftTypeName} ·{" "}
                          {formatDate(request.date)} · {formatTime(request.startTime)}-
                          {formatTime(request.endTime)}
                        </span>
                        {request.message && <p>{request.message}</p>}
                      </div>

                      <div className="my-schedule-shift-actions">
                        <Button
                          type="button"
                          variant="secondary"
                          disabled={isSavingSwap}
                          onClick={() => handleSwapDecision(request.id, "reject")}
                        >
                          Neka
                        </Button>
                        <Button
                          type="button"
                          disabled={isSavingSwap}
                          onClick={() => handleSwapDecision(request.id, "approve")}
                        >
                          Godkänn
                        </Button>
                      </div>
                    </article>
                  ))}
                </section>
              )}

              {sentSwapRequests.length > 0 && (
                <section className="my-schedule-swap-section">
                  <h3>Skickade förfrågningar</h3>
                  {sentSwapRequests.map((request) => (
                    <article className="my-schedule-swap-item" key={request.id}>
                      <div>
                        <strong>{request.toEmployeeName}</strong>
                        <span>
                          {request.shiftTypeName} · {formatDate(request.date)} ·{" "}
                          {formatTime(request.startTime)}-{formatTime(request.endTime)}
                        </span>
                        <Badge variant="info">Väntar</Badge>
                      </div>
                    </article>
                  ))}
                </section>
              )}
            </Card>
          )}

          {groupedShifts.length === 0 ? (
            <Card>
              <p className="my-schedule-muted">
                Du har inga kommande publicerade pass.
              </p>
            </Card>
          ) : (
            <div className="my-schedule-list">
              {groupedShifts.map(([date, shifts]) => (
                <Card key={date} className="my-schedule-day">
                  <h2>{formatDate(date)}</h2>

                  <div className="my-schedule-shifts">
                    {shifts.map((shift) => (
                      <article key={shift.id} className="my-schedule-shift">
                        <div>
                          <strong>{shift.shiftTypeName}</strong>
                          <span>{shift.storeName}</span>
                        </div>

                        <div className="my-schedule-shift-actions">
                          <Badge variant="neutral">
                            {formatTime(shift.startTime)}-{formatTime(shift.endTime)}
                          </Badge>
                          <Button
                            type="button"
                            variant="secondary"
                            onClick={() => openCommentModal(shift)}
                          >
                            Kommentera
                          </Button>
                          <Button
                            type="button"
                            variant="secondary"
                            onClick={() => openSwapModal(shift)}
                          >
                            Föreslå byte
                          </Button>
                        </div>
                      </article>
                    ))}
                  </div>
                </Card>
              ))}
            </div>
          )}
        </>
      )}

      {selectedShift && (
        <div className="my-schedule-modal-backdrop" onClick={closeCommentModal}>
          <section
            className="my-schedule-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="shift-comment-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="my-schedule-modal-header">
              <div>
                <h2 id="shift-comment-modal-title">Kommentera pass</h2>
                <p>
                  {formatDate(selectedShift.date)} · {selectedShift.shiftTypeName} ·{" "}
                  {formatTime(selectedShift.startTime)}-
                  {formatTime(selectedShift.endTime)}
                </p>
              </div>

              <button
                className="my-schedule-modal-close"
                type="button"
                onClick={closeCommentModal}
              >
                Stäng
              </button>
            </div>

            <form className="my-schedule-comment-form" onSubmit={handleCommentSubmit}>
              <label className="input-wrapper">
                <span>Kommentar till admin</span>
                <textarea
                  className="input my-schedule-comment-textarea"
                  maxLength={1000}
                  required
                  value={commentMessage}
                  onChange={(event) => setCommentMessage(event.target.value)}
                />
              </label>

              <div className="my-schedule-modal-actions">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={closeCommentModal}
                >
                  Avbryt
                </Button>
                <Button
                  type="submit"
                  disabled={isSavingComment || !commentMessage.trim()}
                >
                  {isSavingComment ? "Skickar..." : "Skicka kommentar"}
                </Button>
              </div>
            </form>
          </section>
        </div>
      )}

      {swapShift && (
        <div className="my-schedule-modal-backdrop" onClick={closeSwapModal}>
          <section
            className="my-schedule-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="shift-swap-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="my-schedule-modal-header">
              <div>
                <h2 id="shift-swap-modal-title">Föreslå passbyte</h2>
                <p>
                  {formatDate(swapShift.date)} · {swapShift.shiftTypeName} ·{" "}
                  {formatTime(swapShift.startTime)}-{formatTime(swapShift.endTime)}
                </p>
              </div>

              <button
                className="my-schedule-modal-close"
                type="button"
                onClick={closeSwapModal}
              >
                Stäng
              </button>
            </div>

            {isLoadingSwapCandidates ? (
              <p className="my-schedule-muted">Letar lediga kollegor...</p>
            ) : swapCandidates.length === 0 ? (
              <p className="my-schedule-muted">
                Det finns ingen ledig kollega som kan ta det här passet.
              </p>
            ) : (
              <form className="my-schedule-comment-form" onSubmit={handleSwapSubmit}>
                <label className="input-wrapper">
                  <span>Skicka till</span>
                  <select
                    className="input"
                    required
                    value={selectedSwapEmployeeId}
                    onChange={(event) => setSelectedSwapEmployeeId(event.target.value)}
                  >
                    {swapCandidates.map((candidate) => (
                      <option
                        key={candidate.employeeId}
                        value={candidate.employeeId}
                      >
                        {candidate.employeeName} · {candidate.roleName}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="input-wrapper">
                  <span>Meddelande</span>
                  <textarea
                    className="input my-schedule-comment-textarea"
                    maxLength={1000}
                    value={swapMessage}
                    onChange={(event) => setSwapMessage(event.target.value)}
                  />
                </label>

                <div className="my-schedule-modal-actions">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={closeSwapModal}
                  >
                    Avbryt
                  </Button>
                  <Button
                    type="submit"
                    disabled={isSavingSwap || !selectedSwapEmployeeId}
                  >
                    {isSavingSwap ? "Skickar..." : "Skicka bytesförfrågan"}
                  </Button>
                </div>
              </form>
            )}
          </section>
        </div>
      )}
    </main>
  );
}

export default MySchedulePage;
