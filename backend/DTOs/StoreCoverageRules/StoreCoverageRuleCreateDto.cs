namespace Schemalaggning.DTOs.StoreCoverageRules;

public class StoreCoverageRuleCreateDto
{
    public int ShiftTypeId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int RequiredCount { get; set; } = 1;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
