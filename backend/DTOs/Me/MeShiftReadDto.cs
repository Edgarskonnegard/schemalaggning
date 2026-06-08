namespace Schemalaggning.DTOs.Me;

public class MeShiftReadDto
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public string ScheduleName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string ShiftTypeName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}
