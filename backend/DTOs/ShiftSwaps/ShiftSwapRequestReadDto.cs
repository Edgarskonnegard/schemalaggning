namespace Schemalaggning.DTOs.ShiftSwaps;

public class ShiftSwapRequestReadDto
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public int FromEmployeeId { get; set; }

    public string FromEmployeeName { get; set; } = string.Empty;

    public int ToEmployeeId { get; set; }

    public string ToEmployeeName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string ShiftTypeName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }
}
