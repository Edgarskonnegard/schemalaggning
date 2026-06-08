namespace Schemalaggning.Models;

public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public decimal EmploymentPercentage { get; set; }

    public ICollection<BaseScheduleRule> BaseScheduleRules { get; set; } = new List<BaseScheduleRule>();

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public ICollection<ShiftComment> ShiftComments { get; set; } = new List<ShiftComment>();

    public ICollection<ShiftSwapRequest> SentShiftSwapRequests { get; set; } = new List<ShiftSwapRequest>();

    public ICollection<ShiftSwapRequest> ReceivedShiftSwapRequests { get; set; } = new List<ShiftSwapRequest>();

    public ICollection<LeaveAllowance> LeaveAllowances { get; set; } = new List<LeaveAllowance>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public UserAccount? UserAccount { get; set; }
}
