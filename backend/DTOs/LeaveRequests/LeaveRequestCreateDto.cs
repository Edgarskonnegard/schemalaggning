namespace Schemalaggning.DTOs.LeaveRequests;

public class LeaveRequestCreateDto
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;
}
