namespace Schemalaggning.DTOs.BaseScheduleApprovals;

public class BaseScheduleEmployeeSummaryDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public decimal EmploymentPercentage { get; set; }

    public decimal TargetWeeklyHours { get; set; }

    public List<decimal> WeeklyHours { get; set; } = new();

    public decimal TotalHours { get; set; }
}
