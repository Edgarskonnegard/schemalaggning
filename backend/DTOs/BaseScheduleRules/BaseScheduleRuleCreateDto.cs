namespace Schemalaggning.DTOs.BaseScheduleRules;

public class BaseScheduleRuleCreateDto
{
    public DayOfWeek DayOfWeek { get; set; }

    public int ShiftTypeId { get; set; }
}