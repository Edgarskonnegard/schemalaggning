import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { createEmployee, getEmployees } from "../api/employeesApi";
import { createRole, getRoles, updateRole } from "../api/rolesApi";
import { getShiftTypes } from "../api/shiftTypesApi";
import { createStore, generateBaseSchedules, getStores } from "../api/storesApi";
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
  const [stores, setStores] = useState([]);
  const [roles, setRoles] = useState([]);
  const [shiftTypes, setShiftTypes] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState("");
  const [generationResult, setGenerationResult] = useState(null);
  const [newStoreName, setNewStoreName] = useState("");
  const [newRoleName, setNewRoleName] = useState("");
  const [generationStoreId, setGenerationStoreId] = useState("");
  const [editingRoleId, setEditingRoleId] = useState(null);
  const [editingRoleName, setEditingRoleName] = useState("");

  const [formData, setFormData] = useState({
    name: "",
    storeId: "",
    roleId: "",
    employmentPercentage: 100,
    accountEmail: "",
    accountPassword: "",
    accountAccessRole: "Employee",
    accountIsActive: true,
  });

  const shiftTypesByRoleId = useMemo(() => {
    return shiftTypes.reduce((groups, shiftType) => {
      return shiftType.roleIds.reduce((nextGroups, roleId) => {
        const current = nextGroups[roleId] || [];
        return {
          ...nextGroups,
          [roleId]: [...current, shiftType],
        };
      }, groups);
    }, {});
  }, [shiftTypes]);

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [employeesResult, storesResult, rolesResult, shiftTypesResult] =
        await Promise.all([
          getEmployees(),
          getStores(),
          getRoles(),
          getShiftTypes(),
        ]);

      setEmployees(employeesResult);
      setStores(storesResult);
      setRoles(rolesResult);
      setShiftTypes(shiftTypesResult);
      setGenerationStoreId((prev) => prev || storesResult[0]?.id?.toString() || "");
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreateStore(e) {
    e.preventDefault();

    if (!newStoreName.trim()) {
      return;
    }

    try {
      setError("");
      const store = await createStore({ name: newStoreName.trim() });
      setStores((prev) =>
        [...prev, store].sort((a, b) => a.name.localeCompare(b.name))
      );
      setFormData((prev) => ({ ...prev, storeId: String(store.id) }));
      setNewStoreName("");
    } catch (err) {
      setError(err.message);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  function handleChange(e) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: e.target.type === "checkbox" ? e.target.checked : value,
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

  function startEditRole(role) {
    setEditingRoleId(role.id);
    setEditingRoleName(role.name);
  }

  function cancelEditRole() {
    setEditingRoleId(null);
    setEditingRoleName("");
  }

  async function handleUpdateRole(e) {
    e.preventDefault();

    if (!editingRoleName.trim()) {
      return;
    }

    try {
      setError("");
      await updateRole(editingRoleId, { name: editingRoleName.trim() });
      await loadData();
      cancelEditRole();
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();

    if (!formData.name.trim() || !formData.storeId || !formData.roleId) {
      return;
    }

    try {
      setError("");
      const employee = await createEmployee({
        name: formData.name.trim(),
        storeId: Number(formData.storeId),
        roleId: Number(formData.roleId),
        employmentPercentage: Number(formData.employmentPercentage),
        accountEmail: formData.accountEmail.trim(),
        accountPassword: formData.accountPassword,
        accountAccessRole: formData.accountAccessRole,
        accountIsActive: formData.accountIsActive,
      });

      setEmployees((prev) =>
        [...prev, employee].sort((a, b) => a.name.localeCompare(b.name))
      );

      setFormData({
        name: "",
        storeId: formData.storeId,
        roleId: formData.roleId,
        employmentPercentage: 100,
        accountEmail: "",
        accountPassword: "",
        accountAccessRole: "Employee",
        accountIsActive: true,
      });
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleGenerateBaseSchedules(e) {
    e.preventDefault();

    if (!generationStoreId) {
      setError("Välj butik först.");
      return;
    }

    try {
      setError("");
      setGenerationResult(null);
      setIsGenerating(true);
      const result = await generateBaseSchedules(generationStoreId);
      setGenerationResult(result);
      window.dispatchEvent(new Event("approvals-updated"));
    } catch (err) {
      setError(err.message);
    } finally {
      setIsGenerating(false);
    }
  }

  return (
    <main className="employees-page">
      <h1 className="employees-title">Anställda</h1>

      {error && <p className="page-error">{error}</p>}

      <div className="employee-layout">
        <div className="employee-sidebar">
          <section className="form-card">
            <h2 className="section-title">Skapa butik</h2>

            <form onSubmit={handleCreateStore}>
              <div className="form-group">
                <label>Namn</label>

                <input
                  value={newStoreName}
                  onChange={(e) => setNewStoreName(e.target.value)}
                  placeholder="Ex. Centrum"
                />
              </div>

              <button type="submit" className="add-btn">
                Lägg till butik
              </button>
            </form>

            <div className="role-list">
              {stores.length === 0 ? (
                <p className="empty-text">Inga butiker skapade ännu.</p>
              ) : (
                stores.map((store) => (
                  <div key={store.id} className="role-row">
                    <span>{store.name}</span>
                  </div>
                ))
              )}
            </div>
          </section>

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

            <div className="role-list">
              {roles.length === 0 ? (
                <p className="empty-text">Inga roller skapade ännu.</p>
              ) : (
                roles.map((role) => (
                  <div key={role.id} className="role-row">
                    {editingRoleId === role.id ? (
                      <form onSubmit={handleUpdateRole} className="role-edit-form">
                        <input
                          value={editingRoleName}
                          onChange={(e) => setEditingRoleName(e.target.value)}
                        />
                        <div className="role-actions">
                          <button type="submit">Spara</button>
                          <button type="button" onClick={cancelEditRole}>
                            Avbryt
                          </button>
                        </div>
                      </form>
                    ) : (
                      <>
                        <span>{role.name}</span>
                        <button type="button" onClick={() => startEditRole(role)}>
                          Redigera
                        </button>
                      </>
                    )}
                  </div>
                ))
              )}
            </div>
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
                <label>Butik</label>

                <select
                  name="storeId"
                  value={formData.storeId}
                  onChange={handleChange}
                >
                  <option value="">Välj butik</option>
                  {stores.map((store) => (
                    <option key={store.id} value={store.id}>
                      {store.name}
                    </option>
                  ))}
                </select>
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

              <div className="form-group">
                <label>Email för inloggning</label>

                <input
                  type="email"
                  name="accountEmail"
                  value={formData.accountEmail}
                  onChange={handleChange}
                  placeholder="namn@example.com"
                />
              </div>

              <div className="form-group">
                <label>Initialt lösenord</label>

                <input
                  type="password"
                  name="accountPassword"
                  value={formData.accountPassword}
                  onChange={handleChange}
                  placeholder="Minst 8 tecken"
                />
              </div>

              <div className="form-group">
                <label>Access</label>

                <select
                  name="accountAccessRole"
                  value={formData.accountAccessRole}
                  onChange={handleChange}
                >
                  <option value="Employee">Anställd</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>

              <label className="form-checkbox">
                <input
                  type="checkbox"
                  name="accountIsActive"
                  checked={formData.accountIsActive}
                  onChange={handleChange}
                />
                <span>Kontot är aktivt</span>
              </label>

              <button type="submit" className="add-btn">
                Lägg till anställd
              </button>
            </form>
          </section>

          <section className="form-card">
            <h2 className="section-title">Generera grundschema</h2>

            <form onSubmit={handleGenerateBaseSchedules}>
              <div className="form-group">
                <label>Butik</label>

                <select
                  value={generationStoreId}
                  onChange={(e) => setGenerationStoreId(e.target.value)}
                >
                  <option value="">Välj butik</option>
                  {stores.map((store) => (
                    <option key={store.id} value={store.id}>
                      {store.name}
                    </option>
                  ))}
                </select>
              </div>

              <button type="submit" className="add-btn" disabled={isGenerating}>
                {isGenerating ? "Genererar..." : "Generera förslag"}
              </button>
            </form>

            {generationResult && (
              <div className="generation-result">
                <strong>Förslag skapat för godkännande</strong>
                <span>
                  {generationResult.createdRuleCount} regler ·{" "}
                  {generationResult.employeeCount} anställda
                </span>

                {generationResult.unassignedNeedCount > 0 && (
                  <span>{generationResult.unassignedNeedCount} behov kunde inte placeras.</span>
                )}

                {generationResult.warnings?.map((warning) => (
                  <span key={warning}>{warning}</span>
                ))}
              </div>
            )}
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
                      Butik: {employee.storeName}
                    </div>

                    <div className="employee-info">
                      Roll: {employee.roleName}
                    </div>

                    <div className="employee-info">
                      Konto:{" "}
                      {employee.accountEmail
                        ? `${employee.accountEmail} · ${employee.accountAccessRole}`
                        : "Ej skapat"}
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
