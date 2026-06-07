import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { getPendingBaseScheduleApprovalCount } from "../api/approvalsApi";
import { getEmployees } from "../api/employeesApi";
import { getSchedules } from "../api/schedulesApi";
import { getStores } from "../api/storesApi";
import { useAuth } from "../auth/AuthContext";
import "./DashboardPage.css";

function formatDate(value) {
  if (!value) {
    return "";
  }

  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

function getScheduleStatusLabel(status) {
  if (status === "Published") {
    return "Publicerat";
  }

  if (status === "Draft") {
    return "Utkast";
  }

  return status || "Okänd";
}

function DashboardPage() {
  const { isAdmin, user } = useAuth();
  const [overview, setOverview] = useState({
    employees: [],
    stores: [],
    schedules: [],
    pendingApprovals: 0,
  });
  const [isLoading, setIsLoading] = useState(isAdmin);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) {
      setIsLoading(false);
      return;
    }

    async function loadDashboard() {
      setError("");
      setIsLoading(true);

      try {
        const [employees, stores, schedules, pendingApprovals] =
          await Promise.all([
            getEmployees(),
            getStores(),
            getSchedules(),
            getPendingBaseScheduleApprovalCount(),
          ]);

        setOverview({
          employees,
          stores,
          schedules,
          pendingApprovals,
        });
      } catch (err) {
        setError(err.message);
      } finally {
        setIsLoading(false);
      }
    }

    loadDashboard();
  }, [isAdmin]);

  const publishedSchedules = useMemo(
    () => overview.schedules.filter((schedule) => schedule.status === "Published"),
    [overview.schedules]
  );

  const draftSchedules = useMemo(
    () => overview.schedules.filter((schedule) => schedule.status === "Draft"),
    [overview.schedules]
  );

  const latestSchedules = useMemo(() => {
    return [...overview.schedules]
      .sort((a, b) => new Date(b.periodStart) - new Date(a.periodStart))
      .slice(0, 3);
  }, [overview.schedules]);

  if (!isAdmin) {
    return (
      <main className="dashboard-page">
        <section className="dashboard-hero">
          <div>
            <p className="dashboard-eyebrow">Din översikt</p>
            <h1>Hej {user?.email || "där"}</h1>
            <p>
              Här kommer din personliga schemavy hamna när vi kopplar på
              employee-flödet fullt ut.
            </p>
          </div>
          <Link className="dashboard-primary-link" to="/schedule">
            Visa schema
          </Link>
        </section>

        <section className="dashboard-empty-state">
          <h2>Nästa steg för anställda</h2>
          <p>
            När vi bygger user actions kan den här sidan visa kommande pass,
            bytesförfrågningar och ledighetsstatus.
          </p>
        </section>
      </main>
    );
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-hero">
        <div>
          <p className="dashboard-eyebrow">Adminöversikt</p>
          <h1>Dashboard</h1>
          <p>
            Snabb koll på scheman, godkännanden och grunddata innan du går in
            och justerar detaljerna.
          </p>
        </div>

        <div className="dashboard-hero-actions">
          <Link className="dashboard-primary-link" to="/edit-schedule">
            Skapa schema
          </Link>
          <Link className="dashboard-secondary-link" to="/approvals">
            Granska utkast
          </Link>
        </div>
      </section>

      {error && <p className="dashboard-error">{error}</p>}

      <section className="dashboard-stat-grid" aria-label="Snabböversikt">
        <article className="dashboard-stat-card">
          <span>Anställda</span>
          <strong>{isLoading ? "..." : overview.employees.length}</strong>
          <p>Personer som kan schemaläggas.</p>
        </article>

        <article className="dashboard-stat-card">
          <span>Butiker</span>
          <strong>{isLoading ? "..." : overview.stores.length}</strong>
          <p>Enheter med egna bemanningsbehov.</p>
        </article>

        <article className="dashboard-stat-card dashboard-stat-card-warning">
          <span>Att godkänna</span>
          <strong>{isLoading ? "..." : overview.pendingApprovals}</strong>
          <p>Grundscheman som väntar på beslut.</p>
        </article>

        <article className="dashboard-stat-card">
          <span>Publicerade scheman</span>
          <strong>{isLoading ? "..." : publishedSchedules.length}</strong>
          <p>{draftSchedules.length} schemautkast finns kvar.</p>
        </article>
      </section>

      <section className="dashboard-content-grid">
        <article className="dashboard-panel">
          <div className="dashboard-panel-header">
            <div>
              <h2>Senaste scheman</h2>
              <p>De perioder som ligger närmast i arbetet.</p>
            </div>
            <Link to="/edit-schedule">Öppna</Link>
          </div>

          {isLoading ? (
            <p className="dashboard-muted">Laddar scheman...</p>
          ) : latestSchedules.length > 0 ? (
            <div className="dashboard-schedule-list">
              {latestSchedules.map((schedule) => (
                <div className="dashboard-schedule-item" key={schedule.id}>
                  <div>
                    <strong>{schedule.name}</strong>
                    <span>
                      {formatDate(schedule.periodStart)} -{" "}
                      {formatDate(schedule.periodEnd)}
                    </span>
                  </div>
                  <span className={`dashboard-status dashboard-status-${schedule.status?.toLowerCase()}`}>
                    {getScheduleStatusLabel(schedule.status)}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <p className="dashboard-muted">
              Inga scheman finns än. Skapa ett första utkast från grundscheman.
            </p>
          )}
        </article>

        <article className="dashboard-panel">
          <div className="dashboard-panel-header">
            <div>
              <h2>Nästa steg</h2>
              <p>Vanliga flöden för admin.</p>
            </div>
          </div>

          <div className="dashboard-action-list">
            <Link to="/store-coverage">
              <strong>Bemanningsbehov</strong>
              <span>Lägg in passen som butiken behöver täcka.</span>
            </Link>
            <Link to="/employees">
              <strong>Anställda</strong>
              <span>Skapa personal och generera grundscheman.</span>
            </Link>
            <Link to="/schedule-rules">
              <strong>Regler</strong>
              <span>Styr dygnsvila, max dagar i rad och helgfördelning.</span>
            </Link>
          </div>
        </article>
      </section>
    </main>
  );
}

export default DashboardPage;
