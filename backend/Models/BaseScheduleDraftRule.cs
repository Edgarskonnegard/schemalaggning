namespace Schemalaggning.Models;

public class BaseScheduleDraftRule
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public BaseScheduleGenerationBatch Batch { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;

    public int WeekInCycle { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
