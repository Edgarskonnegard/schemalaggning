namespace Schemalaggning.DTOs.BaseScheduleRules;

public class BaseScheduleRuleUpdateDto
{
    public DayOfWeek DayOfWeek { get; set; }

    public int ShiftTypeId { get; set; }
}