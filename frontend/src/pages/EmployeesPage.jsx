import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { createEmployee, getEmployees } from "../api/employeesApi";
import { createRole, getRoles } from "../api/rolesApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import "./EmployeesPage.css";

function getEmploymentLabel(percentage) {
  if (percentage === 100) {
    return "Heltid";
  }

  if (percentage === 0) {
    return "Timanställd";
  }

  return `${percentage}%`;
}

function EmployeesPage() {
  const [employees, setEmployees] = useState([]);
  const [roles, setRoles] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [newRoleName, setNewRoleName] = useState("");

  const [formData, setFormData] = useState({
    name: "",
    roleId: "",
    employmentPercentage: 100,
  });

  const shiftTypesByRoleId = useMemo(() => {
    return shiftTypes.reduce((groups, shiftType) => {
      const current = groups[shiftType.roleId] || [];
      return {
        ...groups,
        [shiftType.roleId]: [...current, shiftType],
      };
    }, {});
  }, [shiftTypes]);

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [employeesResult, rolesResult, shiftTypesResult] =
        await Promise.all([getEmployees(), getRoles(), getShiftTypes()]);

      setEmployees(employeesResult);
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
  }, []);

  function handleChange(e) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  async function handleCreateRole(e) {
    e.preventDefault();

    if (!newRoleName.trim()) {
      return;
    }

    try {
      setError("");
      const role = await createRole({ name: newRoleName.trim() });
      setRoles((prev) => [...prev, role].sort((a, b) => a.name.localeCompare(b.name)));
      setFormData((prev) => ({ ...prev, roleId: String(role.id) }));
      setNewRoleName("");
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();

    if (!formData.name.trim() || !formData.roleId) {
      return;
    }

    try {
      setError("");
      const employee = await createEmployee({
        name: formData.name.trim(),
        roleId: Number(formData.roleId),
        employmentPercentage: Number(formData.employmentPercentage),
      });

      setEmployees((prev) =>
        [...prev, employee].sort((a, b) => a.name.localeCompare(b.name))
      );

      setFormData({
        name: "",
        roleId: formData.roleId,
        employmentPercentage: 100,
      });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <main className="employees-page">
      <h1 className="employees-title">Anställda</h1>

      {error && <p className="page-error">{error}</p>}

      <div className="employee-layout">
        <div className="employee-sidebar">
          <section className="form-card">
            <h2 className="section-title">Skapa roll</h2>

            <form onSubmit={handleCreateRole}>
              <div className="form-group">
                <label>Namn</label>

                <input
                  value={newRoleName}
                  onChange={(e) => setNewRoleName(e.target.value)}
                  placeholder="Ex. Butiksmedarbetare"
                />
              </div>

              <button type="submit" className="add-btn">
                Lägg till roll
              </button>
            </form>
          </section>

          <section className="form-card">
            <h2 className="section-title">Skapa anställd</h2>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Namn</label>

                <input
                  type="text"
                  name="name"
                  value={formData.name}
                  onChange={handleChange}
                  placeholder="Ex. Anna Andersson"
                />
              </div>

              <div className="form-group">
                <label>Roll</label>

                <select
                  name="roleId"
                  value={formData.roleId}
                  onChange={handleChange}
                >
                  <option value="">Välj roll</option>
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
                  value={formData.employmentPercentage}
                  onChange={handleChange}
                />
              </div>

              <button type="submit" className="add-btn">
                Lägg till anställd
              </button>
            </form>
          </section>
        </div>

        <section>
          <h2 className="section-title">Lista över anställda</h2>

          {isLoading ? (
            <p className="empty-text">Laddar anställda...</p>
          ) : employees.length === 0 ? (
            <p className="empty-text">Inga anställda skapade ännu.</p>
          ) : (
            <div className="employee-list">
              {employees.map((employee) => {
                const matchingShiftTypes =
                  shiftTypesByRoleId[employee.roleId] || [];

                return (
                  <Link
                    key={employee.id}
                    to={`/employees/${employee.id}`}
                    className="employee-card"
                  >
                    <div className="employee-header">
                      <span className="employee-name">{employee.name}</span>

                      <span className="badge">
                        {getEmploymentLabel(employee.employmentPercentage)}
                      </span>
                    </div>

                    <div className="employee-info">
                      Roll: {employee.roleName}
                    </div>

                    <div className="shift-access">
                      <h3>Passtyper via roll</h3>

                      {matchingShiftTypes.length === 0 ? (
                        <p className="empty-text">
                          Inga passtyper matchar denna roll.
                        </p>
                      ) : (
                        matchingShiftTypes.map((shiftType) => (
                          <span key={shiftType.id} className="shift-chip">
                            {shiftType.name}
                          </span>
                        ))
                      )}
                    </div>
                  </Link>
                );
              })}
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default EmployeesPage;
