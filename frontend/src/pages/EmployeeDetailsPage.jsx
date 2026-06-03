import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";

import {
  deleteEmployeeBaseScheduleRule,
  setEmployeeBaseScheduleRule,
} from "../api/baseScheduleApi";
import { getEmployeeDetails, updateEmployee } from "../api/employeesApi";
import { getRoles } from "../api/rolesApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import EmployeeBaseSchedule from "../components/employees/EmployeeBaseSchedule";
import "./EmployeeDetailsPage.css";

function getEmploymentLabel(percentage) {
  if (percentage === 100) {
    return "Heltid";
  }

  if (percentage === 0) {
    return "Timanställd";
  }

  return `${percentage}%`;
}

function EmployeeDetailsPage() {
  const { employeeId } = useParams();

  const [employee, setEmployee] = useState(null);
  const [roles, setRoles] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingEmployee, setIsSavingEmployee] = useState(false);
  const [error, setError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const matchingShiftTypes = useMemo(() => {
    if (!employee) {
      return [];
    }

    return shiftTypes.filter((shiftType) =>
      shiftType.roleIds.includes(employee.roleId)
    );
  }, [employee, shiftTypes]);

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [employeeResult, rolesResult, shiftTypesResult] =
        await Promise.all([
          getEmployeeDetails(employeeId),
          getRoles(),
          getShiftTypes(),
        ]);

      setEmployee(employeeResult);
      setRoles(rolesResult);
      setShiftTypes(shiftTypesResult);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [employeeId]);

  function handleEmployeeChange(e) {
    const { name, value } = e.target;

    setEmployee((prev) => ({
      ...prev,
      [name]: name === "employmentPercentage" || name === "roleId"
        ? Number(value)
        : value,
    }));
  }

  async function handleEmployeeSubmit(e) {
    e.preventDefault();

    if (!employee) {
      return;
    }

    try {
      setError("");
      setStatusMessage("");
      setIsSavingEmployee(true);

      await updateEmployee(employee.id, {
        name: employee.name,
        roleId: employee.roleId,
        employmentPercentage: employee.employmentPercentage,
      });

      await loadData();
      setStatusMessage("Personuppgifter sparade.");
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSavingEmployee(false);
    }
  }

  async function handleBaseScheduleChange(changesOrWeek, dayOfWeek, shiftTypeId) {
    if (!employee) {
      return;
    }

    try {
      setError("");
      setStatusMessage("");

      const changes = Array.isArray(changesOrWeek)
        ? changesOrWeek
        : [{ weekInCycle: changesOrWeek, dayOfWeek, shiftTypeId }];

      await Promise.all(
        changes.map((change) => {
          if (!change.shiftTypeId) {
            return deleteEmployeeBaseScheduleRule(
              employee.id,
              change.weekInCycle,
              change.dayOfWeek
            );
          }

          return setEmployeeBaseScheduleRule(employee.id, {
            weekInCycle: change.weekInCycle,
            dayOfWeek: change.dayOfWeek,
            shiftTypeId: Number(change.shiftTypeId),
          });
        })
      );

      const updatedEmployee = await getEmployeeDetails(employee.id);
      setEmployee(updatedEmployee);
      setStatusMessage("Grundschema uppdaterat.");
    } catch (err) {
      setError(`Kunde inte uppdatera grundschema: ${err.message}`);
    }
  }

  if (isLoading) {
    return (
      <main className="employee-details-page">
        <Link to="/employees" className="back-link">
          Tillbaka till anställda
        </Link>

        <p className="empty-text">Laddar anställd...</p>
      </main>
    );
  }

  if (!employee) {
    return (
      <main className="employee-details-page">
        <Link to="/employees" className="back-link">
          Tillbaka till anställda
        </Link>

        <h1>Anställd hittades inte</h1>
        {error && <p className="page-error">{error}</p>}
      </main>
    );
  }

  return (
    <main className="employee-details-page">
      <Link to="/employees" className="back-link">
        Tillbaka till anställda
      </Link>

      {error && <p className="page-error">{error}</p>}
      {statusMessage && <p className="page-status">{statusMessage}</p>}

      <div className="employee-details-header">
        <div>
          <h1>{employee.name}</h1>
          <p>Hantera personuppgifter, roll och fyraveckors grundschema.</p>
        </div>

        <span className="employee-status-badge">
          {getEmploymentLabel(employee.employmentPercentage)}
        </span>
      </div>

      <div className="employee-details-layout">
        <section className="details-card">
          <h2>Personuppgifter</h2>

          <form onSubmit={handleEmployeeSubmit}>
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
              <select
                name="roleId"
                value={employee.roleId}
                onChange={handleEmployeeChange}
              >
                {roles.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Anställningsgrad</label>
              <input
                type="number"
                name="employmentPercentage"
                min="0"
                max="100"
                value={employee.employmentPercentage}
                onChange={handleEmployeeChange}
              />
            </div>

            <button type="submit" className="save-btn" disabled={isSavingEmployee}>
              {isSavingEmployee ? "Sparar..." : "Spara"}
            </button>
          </form>
        </section>

        <section className="details-card">
          <h2>Passtyper via roll</h2>

          {matchingShiftTypes.length === 0 ? (
            <p className="empty-text">
              Det finns inga passtyper som matchar denna roll.
            </p>
          ) : (
            <div className="shift-type-list">
              {matchingShiftTypes.map((shiftType) => (
                <article key={shiftType.id} className="shift-type-option">
                  <span>
                    {shiftType.name}
                    <small>
                      {shiftType.defaultStartTime?.slice(0, 5)}-
                      {shiftType.defaultEndTime?.slice(0, 5)}
                    </small>
                  </span>
                </article>
              ))}
            </div>
          )}
        </section>
      </div>

      <EmployeeBaseSchedule
        baseSchedule={employee.baseSchedule}
        shiftTypes={matchingShiftTypes}
        onChange={handleBaseScheduleChange}
      />
    </main>
  );
}

export default EmployeeDetailsPage;
