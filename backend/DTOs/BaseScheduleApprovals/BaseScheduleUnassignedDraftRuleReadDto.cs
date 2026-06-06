namespace Schemalaggning.DTOs.BaseScheduleApprovals;

public class BaseScheduleUnassignedDraftRuleReadDto
{
    public int Id { get; set; }

    public int ShiftTypeId { get; set; }

    public string ShiftTypeName { get; set; } = string.Empty;

    public int WeekInCycle { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Hours { get; set; }
}
