namespace Schemalaggning.DTOs.Schedules;

public class ScheduleReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<ShiftReadDto> Shifts { get; set; } = new();
}
