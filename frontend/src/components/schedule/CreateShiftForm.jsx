import { useState } from "react";

function normalizeTime(value) {
  return value.length === 5 ? `${value}:00` : value;
}

function CreateShiftForm({ roles, onCreateShiftType }) {
  const [formData, setFormData] = useState({
    name: "",
    roleId: "",
    defaultStartTime: "",
    defaultEndTime: "",
  });

  function handleChange(e) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();

    if (
      !formData.name.trim() ||
      !formData.roleId ||
      !formData.defaultStartTime ||
      !formData.defaultEndTime
    ) {
      return;
    }

    await onCreateShiftType({
      name: formData.name.trim(),
      roleId: Number(formData.roleId),
      defaultStartTime: normalizeTime(formData.defaultStartTime),
      defaultEndTime: normalizeTime(formData.defaultEndTime),
    });

    setFormData({
      name: "",
      roleId: formData.roleId,
      defaultStartTime: "",
      defaultEndTime: "",
    });
  }

  return (
    <section className="shift-form-card">
      <h2>Skapa passtyp</h2>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Namn</label>

          <input
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Ex. Öppning"
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

        <div className="time-row">
          <div className="form-group">
            <label>Start</label>

            <input
              type="time"
              name="defaultStartTime"
              value={formData.defaultStartTime}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label>Slut</label>

            <input
              type="time"
              name="defaultEndTime"
              value={formData.defaultEndTime}
              onChange={handleChange}
            />
          </div>
        </div>

        <button type="submit" className="primary-btn">
          Skapa passtyp
        </button>
      </form>
    </section>
  );
}

export default CreateShiftForm;
