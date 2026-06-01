import { useState } from "react";

function normalizeTime(value) {
  return value.length === 5 ? `${value}:00` : value;
}

function CreateShiftForm({ roles, onCreateShiftType }) {
  const [formData, setFormData] = useState({
    name: "",
    roleIds: [],
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

  function handleRoleToggle(roleId) {
    setFormData((prev) => {
      const roleIds = prev.roleIds.includes(roleId)
        ? prev.roleIds.filter((currentRoleId) => currentRoleId !== roleId)
        : [...prev.roleIds, roleId];

      return {
        ...prev,
        roleIds,
      };
    });
  }

  async function handleSubmit(e) {
    e.preventDefault();

    if (
      !formData.name.trim() ||
      formData.roleIds.length === 0 ||
      !formData.defaultStartTime ||
      !formData.defaultEndTime
    ) {
      return;
    }

    await onCreateShiftType({
      name: formData.name.trim(),
      roleIds: formData.roleIds,
      defaultStartTime: normalizeTime(formData.defaultStartTime),
      defaultEndTime: normalizeTime(formData.defaultEndTime),
    });

    setFormData({
      name: "",
      roleIds: formData.roleIds,
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
          <label>Roller</label>

          <div className="role-checkbox-list">
            {roles.map((role) => (
              <label key={role.id} className="role-checkbox">
                <input
                  type="checkbox"
                  checked={formData.roleIds.includes(role.id)}
                  onChange={() => handleRoleToggle(role.id)}
                />
                <span>{role.name}</span>
              </label>
            ))}
          </div>
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
