namespace Schemalaggning.DTOs.LeaveRequests;

public class LeaveBalanceReadDto
{
    public int Year { get; set; }

    public int TotalDays { get; set; }

    public int UsedDays { get; set; }

    public int RemainingDays { get; set; }

    public int PendingDays { get; set; }
}
