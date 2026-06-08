namespace Schemalaggning.Models;

public class ShiftSwapRequest
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift Shift { get; set; } = null!;

    public int FromEmployeeId { get; set; }

    public Employee FromEmployee { get; set; } = null!;

    public int ToEmployeeId { get; set; }

    public Employee ToEmployee { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAtUtc { get; set; }
}
