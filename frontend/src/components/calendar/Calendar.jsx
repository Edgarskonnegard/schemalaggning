import { useState } from "react";
import "./Calendar.css";

const employees = [
  { id: 1, name: "Anna", role: "Butik" },
  { id: 2, name: "Erik", role: "Kassa" },
  { id: 3, name: "Lisa", role: "Butik" },
  { id: 4, name: "Oskar", role: "Lager" },
];

const initialBaseShifts = [
  {
    id: 1,
    employeeId: 1,
    date: "2026-05-04",
    startTime: "10:00",
    endTime: "18:00",
  },
  {
    id: 2,
    employeeId: 2,
    date: "2026-05-05",
    startTime: "12:00",
    endTime: "20:00",
  },
  {
    id: 3,
    employeeId: 3,
    date: "2026-05-06",
    startTime: "08:00",
    endTime: "16:00",
  },
  {
    id: 4,
    employeeId: 4,
    date: "2026-05-07",
    startTime: "09:00",
    endTime: "17:00",
  },
];

function formatDateKey(date) {
  return date.toISOString().split("T")[0];
}

function getDayLabel(date) {
  return date.toLocaleDateString("sv-SE", {
    weekday: "short",
  });
}

function getMonthLabel(date) {
  return date.toLocaleDateString("sv-SE", {
    month: "short",
  });
}

function getDaysInMonth(year, month) {
  const days = [];
  const numberOfDays = new Date(year, month + 1, 0).getDate();

  for (let day = 1; day <= numberOfDays; day++) {
    days.push(new Date(year, month, day));
  }

  return days;
}

function Calendar() {
  const [currentDate, setCurrentDate] = useState(new Date(2026, 4, 1));
  const [baseShifts] = useState(initialBaseShifts);

  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();

  const monthName = currentDate.toLocaleString("sv-SE", {
    month: "long",
    year: "numeric",
  });

  const days = getDaysInMonth(year, month);

  function getShiftForCell(employeeId, date) {
    const dateKey = formatDateKey(date);

    return baseShifts.find(
      (shift) => shift.employeeId === employeeId && shift.date === dateKey
    );
  }

  function goToPreviousMonth() {
    setCurrentDate(new Date(year, month - 1, 1));
  }

  function goToNextMonth() {
    setCurrentDate(new Date(year, month + 1, 1));
  }

  return (
    <section className="calendar">
      <header className="calendar-header">
        <button onClick={goToPreviousMonth}>←</button>

        <div>
          <h2>{monthName}</h2>
          <p>Grundschema för anställda</p>
        </div>

        <button onClick={goToNextMonth}>→</button>
      </header>

      <div className="calendar-scroll">
        <div
          className="calendar-grid"
          style={{
            gridTemplateColumns: `180px repeat(${days.length}, 120px)`,
          }}
        >
          <div className="calendar-corner">Anställd</div>

          {days.map((date) => (
            <div key={formatDateKey(date)} className="calendar-day-header">
              <span className="calendar-day-name">{getDayLabel(date)}</span>
              <strong>{date.getDate()}</strong>
              <span className="calendar-month-name">{getMonthLabel(date)}</span>
            </div>
          ))}

          {employees.map((employee) => (
            <>
              <div key={`${employee.id}-info`} className="calendar-employee">
                <strong>{employee.name}</strong>
                <span>{employee.role}</span>
              </div>

              {days.map((date) => {
                const shift = getShiftForCell(employee.id, date);

                return (
                  <div
                    key={`${employee.id}-${formatDateKey(date)}`}
                    className="calendar-cell"
                  >
                    {shift ? (
                      <div className="calendar-shift">
                        {shift.startTime} - {shift.endTime}
                      </div>
                    ) : (
                      <span className="calendar-empty">Ledig</span>
                    )}
                  </div>
                );
              })}
            </>
          ))}
        </div>
      </div>
    </section>
  );
}

export default Calendar;