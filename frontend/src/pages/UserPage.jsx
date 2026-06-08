import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { getMe } from "../api/meApi";
import { useAuth } from "../auth/AuthContext";
import Alert from "../components/ui/Alert";
import Badge from "../components/ui/Badge";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import "./UserPage.css";

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

function getEmploymentLabel(percentage) {
  if (percentage === 100) {
    return "Heltid";
  }

  if (percentage === 0) {
    return "Timanställd";
  }

  return `${percentage}%`;
}

function UserPage() {
  const { isAdmin, user } = useAuth();
  const [me, setMe] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadMe() {
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

    loadMe();
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

  return (
    <main className="user-page">
      <PageHeader
        title="Min sida"
        description="Din profil och dina kommande pass."
        actions={<Badge variant={isAdmin ? "warning" : "info"}>{user?.accessRole}</Badge>}
      />

      <Alert>{error}</Alert>

      {isLoading ? (
        <Card>
          <p className="user-muted">Laddar din sida...</p>
        </Card>
      ) : (
        <div className="user-layout">
          <Card>
            <h2>Konto</h2>
            <dl className="user-facts">
              <div>
                <dt>Email</dt>
                <dd>{me?.account.email}</dd>
              </div>
              <div>
                <dt>Access</dt>
                <dd>{me?.account.accessRole}</dd>
              </div>
              <div>
                <dt>Status</dt>
                <dd>{me?.account.isActive ? "Aktivt" : "Inaktivt"}</dd>
              </div>
            </dl>
          </Card>

          {me?.employee ? (
            <Card>
              <h2>Anställning</h2>
              <dl className="user-facts">
                <div>
                  <dt>Namn</dt>
                  <dd>{me.employee.name}</dd>
                </div>
                <div>
                  <dt>Butik</dt>
                  <dd>{me.employee.storeName}</dd>
                </div>
                <div>
                  <dt>Roll</dt>
                  <dd>{me.employee.roleName}</dd>
                </div>
                <div>
                  <dt>Anställningsgrad</dt>
                  <dd>{getEmploymentLabel(me.employee.employmentPercentage)}</dd>
                </div>
              </dl>
            </Card>
          ) : (
            <Card>
              <h2>Admin</h2>
              <p className="user-muted">
                Det här kontot är inte kopplat till en anställd profil.
              </p>
              <div className="user-admin-links">
                <Link to="/employees">Hantera anställda</Link>
                <Link to="/edit-schedule">Skapa schema</Link>
                <Link to="/approvals">Godkännanden</Link>
              </div>
            </Card>
          )}

          <Card className="user-shifts-card">
            <h2>Kommande pass</h2>

            {!me?.employee ? (
              <p className="user-muted">
                Admin-konton visar inget personligt schema här.
              </p>
            ) : groupedShifts.length === 0 ? (
              <p className="user-muted">Du har inga kommande publicerade pass.</p>
            ) : (
              <div className="user-shift-groups">
                {groupedShifts.map(([date, shifts]) => (
                  <section key={date} className="user-shift-day">
                    <h3>{formatDate(date)}</h3>
                    <div className="user-shift-list">
                      {shifts.map((shift) => (
                        <article key={shift.id} className="user-shift-item">
                          <div>
                            <strong>{shift.shiftTypeName}</strong>
                            <span>{shift.storeName}</span>
                          </div>
                          <Badge variant="neutral">
                            {formatTime(shift.startTime)}-{formatTime(shift.endTime)}
                          </Badge>
                        </article>
                      ))}
                    </div>
                  </section>
                ))}
              </div>
            )}
          </Card>
        </div>
      )}
    </main>
  );
}

export default UserPage;
