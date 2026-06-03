namespace Schemalaggning.Models;

public class RoleShiftType
{
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;
}
