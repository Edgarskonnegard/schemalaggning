namespace Schemalaggning.DTOs.BaseScheduleRules;

public class BaseScheduleRuleCreateDto
{
    public int WeekInCycle { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int ShiftTypeId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}
