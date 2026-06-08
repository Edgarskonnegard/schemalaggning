import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { getPendingBaseScheduleApprovalCount } from "../api/approvalsApi";
import { getEmployees } from "../api/employeesApi";
import { getMe } from "../api/meApi";
import { getSchedules } from "../api/schedulesApi";
import { getStores } from "../api/storesApi";
import { useAuth } from "../auth/AuthContext";
import ButtonLink from "../components/ui/ButtonLink";
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

function formatShiftDate(value) {
  if (!value) {
    return "";
  }

  return new Date(`${value}T00:00:00`).toLocaleDateString("sv-SE", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

function formatTime(value) {
  return value?.slice(0, 5) || "";
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
  const [employeeOverview, setEmployeeOverview] = useState(null);
  const [isLoading, setIsLoading] = useState(isAdmin);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadDashboard() {
      setError("");
      setIsLoading(true);

      try {
        if (!isAdmin) {
          setEmployeeOverview(await getMe());
          return;
        }

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

  const nextShifts = useMemo(() => {
    return employeeOverview?.upcomingShifts?.slice(0, 3) ?? [];
  }, [employeeOverview]);

  if (!isAdmin) {
    return (
      <main className="dashboard-page">
        <section className="dashboard-hero">
          <div>
            <p className="dashboard-eyebrow">Din översikt</p>
            <h1>Hej {employeeOverview?.employee?.name || user?.email || "där"}</h1>
            <p>
              Här ser du dina närmaste pass och kommer senare kunna följa
              byten, ledighet och andra personalärenden.
            </p>
          </div>
          <ButtonLink to="/my-schedule">
            Visa mitt schema
          </ButtonLink>
        </section>

        {error && <p className="dashboard-error">{error}</p>}

        <section className="dashboard-panel">
          <div className="dashboard-panel-header">
            <div>
              <h2>Nästkommande pass</h2>
              <p>Dina närmaste publicerade pass.</p>
            </div>
            <Link to="/my-schedule">Alla pass</Link>
          </div>

          {isLoading ? (
            <p className="dashboard-muted">Laddar dina pass...</p>
          ) : !employeeOverview?.employee ? (
            <p className="dashboard-muted">
              Kontot är inte kopplat till en anställd profil ännu.
            </p>
          ) : nextShifts.length === 0 ? (
            <p className="dashboard-muted">
              Du har inga kommande publicerade pass.
            </p>
          ) : (
            <div className="dashboard-next-shifts">
              {nextShifts.map((shift) => (
                <article className="dashboard-next-shift" key={shift.id}>
                  <div>
                    <strong>{shift.shiftTypeName}</strong>
                    <span>{formatShiftDate(shift.date)}</span>
                    <small>{shift.storeName}</small>
                  </div>
                  <span className="dashboard-shift-time">
                    {formatTime(shift.startTime)}-{formatTime(shift.endTime)}
                  </span>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="dashboard-content-grid dashboard-employee-grid">
          <article className="dashboard-panel">
            <div className="dashboard-panel-header">
              <div>
                <h2>Semesterdagar</h2>
                <p>Ditt saldo för innevarande år.</p>
              </div>
              <Link to="/leave">Ansök</Link>
            </div>

            {isLoading ? (
              <p className="dashboard-muted">Laddar semestersaldo...</p>
            ) : employeeOverview?.leaveBalance ? (
              <div className="dashboard-leave-balance">
                <div>
                  <span>Kvar</span>
                  <strong>{employeeOverview.leaveBalance.remainingDays}</strong>
                </div>
                <div>
                  <span>Använda</span>
                  <strong>{employeeOverview.leaveBalance.usedDays}</strong>
                </div>
                <div>
                  <span>Väntar</span>
                  <strong>{employeeOverview.leaveBalance.pendingDays}</strong>
                </div>
              </div>
            ) : (
              <p className="dashboard-muted">
                Inget semestersaldo hittades för kontot.
              </p>
            )}
          </article>

          <article className="dashboard-panel">
            <div className="dashboard-panel-header">
              <div>
                <h2>Kommande funktioner</h2>
                <p>Här kommer status för byten och ledighet visas.</p>
              </div>
            </div>
            <p className="dashboard-muted">
              Nästa steg är admin-godkännande av ledighetsansökningar.
            </p>
          </article>
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
          <ButtonLink to="/edit-schedule">
            Skapa schema
          </ButtonLink>
          <ButtonLink to="/approvals" variant="secondary">
            Granska utkast
          </ButtonLink>
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
