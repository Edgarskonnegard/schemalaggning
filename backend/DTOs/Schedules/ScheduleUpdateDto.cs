namespace Schemalaggning.DTOs.Schedules;

public class ScheduleUpdateDto
{
    public string Name { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}