namespace Schemalaggning.Models;

public class Shift
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public Schedule Schedule { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Source { get; set; } = "BaseSchedule";

    public string Status { get; set; } = "Draft";

    public ICollection<ShiftComment> Comments { get; set; } = new List<ShiftComment>();
}
