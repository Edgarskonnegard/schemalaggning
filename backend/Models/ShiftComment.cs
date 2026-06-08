namespace Schemalaggning.Models;

public class ShiftComment
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift Shift { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "New";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAtUtc { get; set; }

    public int? ResolvedByUserAccountId { get; set; }

    public UserAccount? ResolvedByUserAccount { get; set; }
}
