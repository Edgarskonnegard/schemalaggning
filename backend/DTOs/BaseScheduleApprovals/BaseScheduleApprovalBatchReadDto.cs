namespace Schemalaggning.DTOs.BaseScheduleApprovals;

public class BaseScheduleApprovalBatchReadDto
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public int DraftRuleCount { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<BaseScheduleDraftRuleReadDto> DraftRules { get; set; } = new();

    public List<BaseScheduleEmployeeSummaryDto> EmployeeSummaries { get; set; } = new();

    public List<BaseScheduleEmployeeApprovalReadDto> EmployeeApprovals { get; set; } = new();

    public List<BaseScheduleUnassignedDraftRuleReadDto> UnassignedDraftRules { get; set; } = new();
}
