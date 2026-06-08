namespace Schemalaggning.DTOs.Schedules;

public class ScheduleCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}
