import { useState } from "react";
import "./EmployeesPage.css";
import { Link } from "react-router-dom";

const shiftTypes = [
  {
    id: 1,
    name: "Öppning",
    role: "Butiksmedarbetare",
  },
  {
    id: 2,
    name: "Stängning",
    role: "Butiksmedarbetare",
  },
  {
    id: 3,
    name: "Kassa",
    role: "Kassa",
  },
];

function EmployeesPage() {
  const [employees, setEmployees] = useState([
    {
      id: 1,
      name: "Anna",
      role: "Butiksmedarbetare",
      employmentType: "Heltid",
      allowedShiftTypeIds: [1],
    },
    {
      id: 2,
      name: "Erik",
      role: "Butiksmedarbetare",
      employmentType: "Deltid",
      allowedShiftTypeIds: [2],
    },
  ]);

  const [formData, setFormData] = useState({
    name: "",
    role: "",
    employmentType: "Heltid",
  });

  function handleChange(e) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  function handleSubmit(e) {
    e.preventDefault();

    if (!formData.name.trim() || !formData.role.trim()) {
      return;
    }

    const newEmployee = {
      id: Date.now(),
      name: formData.name,
      role: formData.role,
      employmentType: formData.employmentType,
      allowedShiftTypeIds: [],
    };

    setEmployees((prev) => [...prev, newEmployee]);

    setFormData({
      name: "",
      role: "",
      employmentType: "Heltid",
    });
  }

  function handleShiftTypeToggle(employeeId, shiftTypeId) {
    setEmployees((prev) =>
      prev.map((employee) => {
        if (employee.id !== employeeId) {
          return employee;
        }

        const alreadyAllowed =
          employee.allowedShiftTypeIds.includes(shiftTypeId);

        return {
          ...employee,
          allowedShiftTypeIds: alreadyAllowed
            ? employee.allowedShiftTypeIds.filter((id) => id !== shiftTypeId)
            : [...employee.allowedShiftTypeIds, shiftTypeId],
        };
      })
    );
  }

  return (
    <main className="employees-page">
      <h1 className="employees-title">Anställda</h1>

      <div className="employee-layout">
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

              <input
                type="text"
                name="role"
                value={formData.role}
                onChange={handleChange}
                placeholder="Ex. Butiksmedarbetare"
              />
            </div>

            <div className="form-group">
              <label>Anställningstyp</label>

              <select
                name="employmentType"
                value={formData.employmentType}
                onChange={handleChange}
              >
                <option value="Heltid">Heltid</option>
                <option value="Deltid">Deltid</option>
                <option value="Timanställd">Timanställd</option>
              </select>
            </div>

            <button type="submit" className="add-btn">
              Lägg till anställd
            </button>
          </form>
        </section>

        <section>
          <h2 className="section-title">Lista över anställda</h2>

          <div className="employee-list">
            {employees.map((employee) => {
              const matchingShiftTypes = shiftTypes.filter(
                (shiftType) => shiftType.role === employee.role
              );

              return (
                <Link
                    key={employee.id}
                    to={`/employees/${employee.id}`}
                    className="employee-card"
                    >
                  <div className="employee-header">
                    <span className="employee-name">{employee.name}</span>

                    <span className="badge">{employee.employmentType}</span>
                  </div>

                  <div className="employee-info">Roll: {employee.role}</div>

                  <div className="shift-access">
                    <h3>Tillåtna passtyper</h3>

                    {matchingShiftTypes.length === 0 ? (
                      <p className="empty-text">
                        Inga passtyper matchar denna roll.
                      </p>
                    ) : (
                      matchingShiftTypes.map((shiftType) => (
                        <label
                          key={shiftType.id}
                          className="shift-access-option"
                        >
                          <input
                            type="checkbox"
                            checked={employee.allowedShiftTypeIds.includes(
                              shiftType.id
                            )}
                            onChange={() =>
                              handleShiftTypeToggle(employee.id, shiftType.id)
                            }
                          />

                          <span>{shiftType.name}</span>
                        </label>
                      ))
                    )}
                  </div>
                </Link>
              );
            })}
          </div>
        </section>
      </div>
    </main>
  );
}

export default EmployeesPage;