import { useEffect, useMemo, useState } from "react";

import { getMe } from "../api/meApi";
import { createShiftComment } from "../api/shiftCommentsApi";
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
  const [selectedShift, setSelectedShift] = useState(null);
  const [commentMessage, setCommentMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingComment, setIsSavingComment] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [error, setError] = useState("");

  async function loadSchedule() {
    setError("");
    setIsLoading(true);

    try {
      setMe(await getMe());
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
      ) : groupedShifts.length === 0 ? (
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
                    </div>
                  </article>
                ))}
              </div>
            </Card>
          ))}
        </div>
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
    </main>
  );
}

export default MySchedulePage;
