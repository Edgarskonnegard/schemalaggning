namespace Schemalaggning.Models;

public class ShiftType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }

    public ICollection<EmployeeShiftType> EmployeeShiftTypes { get; set; } = new List<EmployeeShiftType>();

    public ICollection<BaseScheduleRule> BaseScheduleRules { get; set; } = new List<BaseScheduleRule>();

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}