import { useState } from "react";

import Button from "../ui/Button";
import Card from "../ui/Card";
import Input from "../ui/Input";

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
    <Card>
      <h2>Skapa passtyp</h2>

      <form onSubmit={handleSubmit}>
        <Input
          label="Namn"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="Ex. Öppning"
        />

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
          <Input
            label="Start"
            type="time"
            name="defaultStartTime"
            value={formData.defaultStartTime}
            onChange={handleChange}
          />

          <Input
            label="Slut"
            type="time"
            name="defaultEndTime"
            value={formData.defaultEndTime}
            onChange={handleChange}
          />
        </div>

        <Button type="submit" fullWidth>
          Skapa passtyp
        </Button>
      </form>
    </Card>
  );
}

export default CreateShiftForm;
