namespace Schemalaggning.DTOs.StoreCoverageRules;

public class StoreCoverageRuleCreateDto
{
    public int ShiftTypeId { get; set; }

    public int WeekInCycle { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int RequiredCount { get; set; } = 1;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
