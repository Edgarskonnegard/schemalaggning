import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import EmployeeBaseSchedule from "../components/employees/EmployeeBaseSchedule";
import "./EmployeeDetailsPage.css";

const shiftTypes = [
  {
    id: 1,
    name: "Öppning",
    role: "Butiksmedarbetare",
    defaultStartTime: "08:00",
    defaultEndTime: "16:00",
  },
  {
    id: 2,
    name: "Stängning",
    role: "Butiksmedarbetare",
    defaultStartTime: "12:00",
    defaultEndTime: "20:00",
  },
  {
    id: 3,
    name: "Kassa",
    role: "Kassa",
    defaultStartTime: "10:00",
    defaultEndTime: "18:00",
  },
];

const initialEmployees = [
  {
    id: 1,
    name: "Anna",
    role: "Butiksmedarbetare",
    employmentType: "Heltid",
    allowedShiftTypeIds: [1, 2],
    baseSchedule: [
      {
        id: 1,
        day: "monday",
        shiftTypeId: 1,
      },
      {
        id: 2,
        day: "tuesday",
        shiftTypeId: 2,
      },
    ],
  },
  {
    id: 2,
    name: "Erik",
    role: "Butiksmedarbetare",
    employmentType: "Deltid",
    allowedShiftTypeIds: [2],
    baseSchedule: [
      {
        id: 3,
        day: "wednesday",
        shiftTypeId: 2,
      },
    ],
  },
];

function EmployeeDetailsPage() {
  const { employeeId } = useParams();

  const [employees, setEmployees] = useState(initialEmployees);

  const employee = employees.find(
    (employee) => employee.id === Number(employeeId)
  );

  function handleEmployeeChange(e) {
    const { name, value } = e.target;

    setEmployees((prev) =>
      prev.map((currentEmployee) =>
        currentEmployee.id === employee.id
          ? {
              ...currentEmployee,
              [name]: value,
            }
          : currentEmployee
      )
    );
  }

  function handleAllowedShiftTypeToggle(shiftTypeId) {
    setEmployees((prev) =>
      prev.map((currentEmployee) => {
        if (currentEmployee.id !== employee.id) {
          return currentEmployee;
        }

        const alreadyAllowed =
          currentEmployee.allowedShiftTypeIds.includes(shiftTypeId);

        return {
          ...currentEmployee,
          allowedShiftTypeIds: alreadyAllowed
            ? currentEmployee.allowedShiftTypeIds.filter(
                (id) => id !== shiftTypeId
              )
            : [...currentEmployee.allowedShiftTypeIds, shiftTypeId],
        };
      })
    );
  }

  function handleBaseScheduleChange(day, shiftTypeId) {
    setEmployees((prev) =>
      prev.map((currentEmployee) => {
        if (currentEmployee.id !== employee.id) {
          return currentEmployee;
        }

        const withoutCurrentDay =
          currentEmployee.baseSchedule.filter(
            (rule) => rule.day !== day
          );

        if (!shiftTypeId) {
          return {
            ...currentEmployee,
            baseSchedule: withoutCurrentDay,
          };
        }

        return {
          ...currentEmployee,
          baseSchedule: [
            ...withoutCurrentDay,
            {
              id: Date.now(),
              day,
              shiftTypeId: Number(shiftTypeId),
            },
          ],
        };
      })
    );
  }

  if (!employee) {
    return (
      <main className="employee-details-page">
        <Link to="/employees" className="back-link">
          ← Tillbaka till anställda
        </Link>

        <h1>Anställd hittades inte</h1>
      </main>
    );
  }

  const matchingShiftTypes = shiftTypes.filter(
    (shiftType) => shiftType.role === employee.role
  );

  const allowedShiftTypes = shiftTypes.filter((shiftType) =>
    employee.allowedShiftTypeIds.includes(shiftType.id)
  );

  return (
    <main className="employee-details-page">
      <Link to="/employees" className="back-link">
        ← Tillbaka till anställda
      </Link>

      <div className="employee-details-header">
        <div>
          <h1>{employee.name}</h1>
          <p>
            Hantera personuppgifter, tillåtna passtyper och
            grundschema för den anställda.
          </p>
        </div>

        <span className="employee-status-badge">
          {employee.employmentType}
        </span>
      </div>

      <div className="employee-details-layout">
        <section className="details-card">
          <h2>Personuppgifter</h2>

          <div className="form-group">
            <label>Namn</label>
            <input
              name="name"
              value={employee.name}
              onChange={handleEmployeeChange}
            />
          </div>

          <div className="form-group">
            <label>Roll</label>
            <input
              name="role"
              value={employee.role}
              onChange={handleEmployeeChange}
            />
          </div>

          <div className="form-group">
            <label>Anställningstyp</label>
            <select
              name="employmentType"
              value={employee.employmentType}
              onChange={handleEmployeeChange}
            >
              <option value="Heltid">Heltid</option>
              <option value="Deltid">Deltid</option>
              <option value="Timanställd">Timanställd</option>
            </select>
          </div>
        </section>

        <section className="details-card">
          <h2>Tillåtna passtyper</h2>

          {matchingShiftTypes.length === 0 ? (
            <p className="empty-text">
              Det finns inga passtyper som matchar denna roll.
            </p>
          ) : (
            <div className="shift-type-list">
              {matchingShiftTypes.map((shiftType) => (
                <label
                  key={shiftType.id}
                  className="shift-type-option"
                >
                  <input
                    type="checkbox"
                    checked={employee.allowedShiftTypeIds.includes(
                      shiftType.id
                    )}
                    onChange={() =>
                      handleAllowedShiftTypeToggle(shiftType.id)
                    }
                  />

                  <span>
                    {shiftType.name}{" "}
                    <small>
                      {shiftType.defaultStartTime}-
                      {shiftType.defaultEndTime}
                    </small>
                  </span>
                </label>
              ))}
            </div>
          )}
        </section>
      </div>

      <EmployeeBaseSchedule
        baseSchedule={employee.baseSchedule}
        shiftTypes={allowedShiftTypes}
        onChange={handleBaseScheduleChange}
      />
    </main>
  );
}

export default EmployeeDetailsPage;