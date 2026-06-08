import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";

import {
  deleteEmployeeBaseScheduleRule,
  setEmployeeBaseScheduleRule,
} from "../api/baseScheduleApi";
import { getEmployeeDetails, updateEmployee } from "../api/employeesApi";
import { getRoles } from "../api/rolesApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import { getStores } from "../api/storesApi";
import EmployeeBaseSchedule from "../components/employees/EmployeeBaseSchedule";
import Alert from "../components/ui/Alert";
import Badge from "../components/ui/Badge";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Input from "../components/ui/Input";
import PageHeader from "../components/ui/PageHeader";
import Select from "../components/ui/Select";
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
  const [stores, setStores] = useState([]);
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
      const [employeeResult, storesResult, rolesResult, shiftTypesResult] =
        await Promise.all([
          getEmployeeDetails(employeeId),
          getStores(),
          getRoles(),
          getShiftTypes(),
        ]);

      setEmployee(employeeResult);
      setStores(storesResult);
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
      [name]:
        e.target.type === "checkbox"
          ? e.target.checked
          : name === "employmentPercentage" || name === "roleId" || name === "storeId"
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
        storeId: employee.storeId,
        roleId: employee.roleId,
        employmentPercentage: employee.employmentPercentage,
        accountEmail: employee.accountEmail,
        accountPassword: employee.accountPassword || "",
        accountAccessRole: employee.accountAccessRole || "Employee",
        accountIsActive: employee.accountIsActive,
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
            startTime: change.startTime,
            endTime: change.endTime,
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
        <Alert>{error}</Alert>
      </main>
    );
  }

  return (
    <main className="employee-details-page">
      <Link to="/employees" className="back-link">
        Tillbaka till anställda
      </Link>

      <Alert>{error}</Alert>
      <Alert variant="success">{statusMessage}</Alert>

      <PageHeader
        title={employee.name}
        description="Hantera personuppgifter, roll och fyraveckors grundschema."
        actions={
          <Badge>{getEmploymentLabel(employee.employmentPercentage)}</Badge>
        }
      />

      <div className="employee-details-layout">
        <Card>
          <h2>Personuppgifter</h2>

          <form onSubmit={handleEmployeeSubmit}>
            <Input
              label="Namn"
              name="name"
              value={employee.name}
              onChange={handleEmployeeChange}
            />

            <Select
              label="Butik"
              name="storeId"
              value={employee.storeId}
              onChange={handleEmployeeChange}
            >
              {stores.map((store) => (
                <option key={store.id} value={store.id}>
                  {store.name}
                </option>
              ))}
            </Select>

            <Select
              label="Roll"
              name="roleId"
              value={employee.roleId}
              onChange={handleEmployeeChange}
            >
              {roles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </Select>

            <Input
              label="Anställningsgrad"
              type="number"
              name="employmentPercentage"
              min="0"
              max="100"
              value={employee.employmentPercentage}
              onChange={handleEmployeeChange}
            />

            <Button type="submit" fullWidth disabled={isSavingEmployee}>
              {isSavingEmployee ? "Sparar..." : "Spara"}
            </Button>
          </form>
        </Card>

        <Card>
          <h2>Inloggning</h2>

          <form onSubmit={handleEmployeeSubmit}>
            <Input
              label="Email"
              type="email"
              name="accountEmail"
              value={employee.accountEmail || ""}
              onChange={handleEmployeeChange}
              placeholder="namn@example.com"
            />

            <Input
              label="Nytt lösenord"
              type="password"
              name="accountPassword"
              value={employee.accountPassword || ""}
              onChange={handleEmployeeChange}
              placeholder={
                employee.accountId
                  ? "Lämna tomt för att behålla"
                  : "Minst 8 tecken"
              }
            />

            <Select
              label="Access"
              name="accountAccessRole"
              value={employee.accountAccessRole || "Employee"}
              onChange={handleEmployeeChange}
            >
              <option value="Employee">Anställd</option>
              <option value="Admin">Admin</option>
            </Select>

            <label className="details-checkbox">
              <input
                type="checkbox"
                name="accountIsActive"
                checked={Boolean(employee.accountIsActive)}
                onChange={handleEmployeeChange}
              />
              <span>Kontot är aktivt</span>
            </label>

            <Button type="submit" fullWidth disabled={isSavingEmployee}>
              {isSavingEmployee ? "Sparar..." : "Spara inloggning"}
            </Button>
          </form>
        </Card>

        <Card>
          <h2>Passtyper via roll</h2>

          {matchingShiftTypes.length === 0 ? (
            <p className="empty-text">
              Det finns inga passtyper som matchar denna roll.
            </p>
          ) : (
            <div className="shift-type-list">
              {matchingShiftTypes.map((shiftType) => (
                <Badge key={shiftType.id} variant="neutral" className="shift-type-option">
                  {shiftType.name}
                  <small>
                    {shiftType.defaultStartTime?.slice(0, 5)}-
                    {shiftType.defaultEndTime?.slice(0, 5)}
                  </small>
                </Badge>
              ))}
            </div>
          )}
        </Card>
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
