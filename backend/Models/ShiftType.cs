namespace Schemalaggning.Models;

public class ShiftType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }

    public ICollection<RoleShiftType> RoleShiftTypes { get; set; } = new List<RoleShiftType>();

    public ICollection<BaseScheduleRule> BaseScheduleRules { get; set; } = new List<BaseScheduleRule>();

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
