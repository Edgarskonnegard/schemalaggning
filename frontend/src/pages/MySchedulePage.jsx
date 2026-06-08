import { useEffect, useMemo, useState } from "react";

import { getMe } from "../api/meApi";
import Alert from "../components/ui/Alert";
import Badge from "../components/ui/Badge";
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
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
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

  return (
    <main className="my-schedule-page">
      <PageHeader
        title="Mitt schema"
        description="Dina kommande publicerade pass."
      />

      <Alert>{error}</Alert>

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

                    <Badge variant="neutral">
                      {formatTime(shift.startTime)}-{formatTime(shift.endTime)}
                    </Badge>
                  </article>
                ))}
              </div>
            </Card>
          ))}
        </div>
      )}
    </main>
  );
}

export default MySchedulePage;
