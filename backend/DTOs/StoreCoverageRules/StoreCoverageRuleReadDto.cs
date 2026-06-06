namespace Schemalaggning.DTOs.StoreCoverageRules;

public class StoreCoverageRuleReadDto
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public int ShiftTypeId { get; set; }

    public string ShiftTypeName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public int RequiredCount { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
