import { useState } from "react";

import Button from "../ui/Button";
import Card from "../ui/Card";
import Input from "../ui/Input";

function formatTime(value) {
  return value?.slice(0, 5) || "";
}

function normalizeTime(value) {
  return value.length === 5 ? `${value}:00` : value;
}

function ScheduleList({ roles, shifts, onUpdateShiftType }) {
  const [editingId, setEditingId] = useState(null);
  const [editData, setEditData] = useState(null);

  function startEdit(shift) {
    setEditingId(shift.id);
    setEditData({
      name: shift.name,
      roleIds: shift.roleIds,
      defaultStartTime: formatTime(shift.defaultStartTime),
      defaultEndTime: formatTime(shift.defaultEndTime),
    });
  }

  function cancelEdit() {
    setEditingId(null);
    setEditData(null);
  }

  function handleChange(e) {
    const { name, value } = e.target;
    setEditData((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  function handleRoleToggle(roleId) {
    setEditData((prev) => {
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

    await onUpdateShiftType(editingId, {
      name: editData.name.trim(),
      roleIds: editData.roleIds,
      defaultStartTime: normalizeTime(editData.defaultStartTime),
      defaultEndTime: normalizeTime(editData.defaultEndTime),
    });

    cancelEdit();
  }

  return (
    <section>
      <h2>Skapade passtyper</h2>

      <div className="shift-list">
        {shifts.length === 0 ? (
          <p className="empty-text">Inga passtyper skapade ännu.</p>
        ) : (
          shifts.map((shift) => {
            const isEditing = editingId === shift.id;

            return (
              <Card key={shift.id} className="shift-card">
                {isEditing ? (
                  <form onSubmit={handleSubmit} className="shift-edit-form">
                    <Input
                      label="Namn"
                      name="name"
                      value={editData.name}
                      onChange={handleChange}
                    />

                    <div className="time-row">
                      <Input
                        label="Start"
                        type="time"
                        name="defaultStartTime"
                        value={editData.defaultStartTime}
                        onChange={handleChange}
                      />

                      <Input
                        label="Slut"
                        type="time"
                        name="defaultEndTime"
                        value={editData.defaultEndTime}
                        onChange={handleChange}
                      />
                    </div>

                    <div className="form-group">
                      <label>Roller</label>
                      <div className="role-checkbox-list">
                        {roles.map((role) => (
                          <label key={role.id} className="role-checkbox">
                            <input
                              type="checkbox"
                              checked={editData.roleIds.includes(role.id)}
                              onChange={() => handleRoleToggle(role.id)}
                            />
                            <span>{role.name}</span>
                          </label>
                        ))}
                      </div>
                    </div>

                    <div className="button-row">
                      <Button type="submit">
                        Spara
                      </Button>
                      <Button
                        type="button"
                        variant="secondary"
                        onClick={cancelEdit}
                      >
                        Avbryt
                      </Button>
                    </div>
                  </form>
                ) : (
                  <>
                    <h3>{shift.name}</h3>
                    <p>Roller: {shift.roleNames.join(", ")}</p>
                    <p>
                      {formatTime(shift.defaultStartTime)}-
                      {formatTime(shift.defaultEndTime)}
                    </p>

                    <Button
                      type="button"
                      variant="secondary"
                      onClick={() => startEdit(shift)}
                    >
                      Redigera
                    </Button>
                  </>
                )}
              </Card>
            );
          })
        )}
      </div>
    </section>
  );
}

export default ScheduleList;
