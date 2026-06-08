namespace Schemalaggning.Models;

public class BaseScheduleEmployeeApproval
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public BaseScheduleGenerationBatch Batch { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public string Status { get; set; } = "Pending";

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }
}
