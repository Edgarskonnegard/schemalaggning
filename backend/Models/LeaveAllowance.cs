namespace Schemalaggning.Models;

public class LeaveAllowance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int Year { get; set; }

    public int TotalDays { get; set; } = 25;
}
