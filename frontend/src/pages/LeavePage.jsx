import { useEffect, useMemo, useState } from "react";

import {
  createMyLeaveRequest,
  getMyLeaveBalance,
  getMyLeaveRequests,
} from "../api/leaveRequestsApi";
import Alert from "../components/ui/Alert";
import Badge from "../components/ui/Badge";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Input from "../components/ui/Input";
import PageHeader from "../components/ui/PageHeader";
import "./LeavePage.css";

function toDateInputValue(date) {
  return date.toISOString().slice(0, 10);
}

function getToday() {
  return toDateInputValue(new Date());
}

function formatDate(value) {
  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function getStatusLabel(status) {
  if (status === "Pending") return "Väntar";
  if (status === "Approved") return "Godkänd";
  if (status === "Rejected") return "Nekad";
  return status;
}

function getStatusVariant(status) {
  if (status === "Approved") return "success";
  if (status === "Rejected") return "warning";
  return "info";
}

function LeavePage() {
  const [requests, setRequests] = useState([]);
  const [balance, setBalance] = useState(null);
  const [form, setForm] = useState({
    startDate: getToday(),
    endDate: getToday(),
    reason: "",
  });
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [error, setError] = useState("");

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [requestsResult, balanceResult] = await Promise.all([
        getMyLeaveRequests(),
        getMyLeaveBalance(),
      ]);

      setRequests(requestsResult);
      setBalance(balanceResult);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const sortedRequests = useMemo(() => {
    return [...requests].sort(
      (a, b) => new Date(b.startDate) - new Date(a.startDate)
    );
  }, [requests]);

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
      ...(field === "startDate" && prev.endDate < value ? { endDate: value } : {}),
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setStatusMessage("");
    setIsSaving(true);

    try {
      await createMyLeaveRequest({
        startDate: form.startDate,
        endDate: form.endDate,
        reason: form.reason.trim(),
      });
      setForm({
        startDate: getToday(),
        endDate: getToday(),
        reason: "",
      });
      setStatusMessage("Ledighetsansökan skickad.");
      await loadData();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="leave-page">
      <PageHeader
        title="Ledighet"
        description="Ansök om semester och följ status på dina ledighetsansökningar."
      />

      <Alert>{error}</Alert>
      <Alert variant="success">{statusMessage}</Alert>

      <section className="leave-layout">
        <Card className="leave-balance-card">
          <h2>Semesterdagar</h2>

          {isLoading ? (
            <p className="leave-muted">Laddar saldo...</p>
          ) : balance ? (
            <div className="leave-balance-grid">
              <div>
                <span>Kvar</span>
                <strong>{balance.remainingDays}</strong>
              </div>
              <div>
                <span>Använda</span>
                <strong>{balance.usedDays}</strong>
              </div>
              <div>
                <span>Väntar</span>
                <strong>{balance.pendingDays}</strong>
              </div>
              <div>
                <span>Totalt {balance.year}</span>
                <strong>{balance.totalDays}</strong>
              </div>
            </div>
          ) : (
            <p className="leave-muted">Kunde inte läsa semestersaldo.</p>
          )}
        </Card>

        <Card>
          <h2>Ny ansökan</h2>

          <form className="leave-form" onSubmit={handleSubmit}>
            <Input
              label="Från"
              type="date"
              min={getToday()}
              value={form.startDate}
              onChange={(event) => updateForm("startDate", event.target.value)}
            />

            <Input
              label="Till"
              type="date"
              min={form.startDate}
              value={form.endDate}
              onChange={(event) => updateForm("endDate", event.target.value)}
            />

            <label className="leave-reason">
              <span>Kommentar</span>
              <textarea
                value={form.reason}
                onChange={(event) => updateForm("reason", event.target.value)}
                placeholder="Valfri kommentar till admin"
              />
            </label>

            <Button type="submit" disabled={isSaving || isLoading}>
              {isSaving ? "Skickar..." : "Skicka ansökan"}
            </Button>
          </form>
        </Card>
      </section>

      <Card>
        <h2>Mina ansökningar</h2>

        {isLoading ? (
          <p className="leave-muted">Laddar ansökningar...</p>
        ) : sortedRequests.length === 0 ? (
          <p className="leave-muted">Du har inte skickat några ansökningar än.</p>
        ) : (
          <div className="leave-request-list">
            {sortedRequests.map((request) => (
              <article className="leave-request-item" key={request.id}>
                <div>
                  <strong>
                    {formatDate(request.startDate)} - {formatDate(request.endDate)}
                  </strong>
                  <span>{request.requestedDays} semesterdagar</span>
                  {request.reason && <small>{request.reason}</small>}
                  {request.adminComment && <small>{request.adminComment}</small>}
                </div>

                <Badge variant={getStatusVariant(request.status)}>
                  {getStatusLabel(request.status)}
                </Badge>
              </article>
            ))}
          </div>
        )}
      </Card>
    </main>
  );
}

export default LeavePage;
