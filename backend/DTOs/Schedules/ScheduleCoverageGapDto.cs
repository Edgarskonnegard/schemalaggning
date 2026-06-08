namespace Schemalaggning.DTOs.Schedules;

public class ScheduleCoverageGapDto
{
    public DateOnly Date { get; set; }

    public int ShiftTypeId { get; set; }

    public string ShiftTypeName { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int RequiredCount { get; set; }

    public int AssignedCount { get; set; }

    public int MissingCount { get; set; }
}
