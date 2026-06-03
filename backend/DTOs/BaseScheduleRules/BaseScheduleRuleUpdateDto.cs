namespace Schemalaggning.DTOs.BaseScheduleRules;

public class BaseScheduleRuleUpdateDto
{
    public int WeekInCycle { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int ShiftTypeId { get; set; }
}
