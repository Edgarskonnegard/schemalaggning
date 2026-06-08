namespace Schemalaggning.Models;

public class BaseScheduleGenerationBatch
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string WarningText { get; set; } = string.Empty;

    public ICollection<BaseScheduleDraftRule> DraftRules { get; set; } = new List<BaseScheduleDraftRule>();

    public ICollection<BaseScheduleEmployeeApproval> EmployeeApprovals { get; set; } = new List<BaseScheduleEmployeeApproval>();

    public ICollection<BaseScheduleUnassignedDraftRule> UnassignedDraftRules { get; set; } = new List<BaseScheduleUnassignedDraftRule>();
}
