namespace Schemalaggning.DTOs.BaseScheduleApprovals;

public class BaseScheduleEmployeeApprovalReadDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public decimal EmploymentPercentage { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }
}
