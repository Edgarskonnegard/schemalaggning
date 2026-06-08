using Schemalaggning.DTOs.Auth;
using Schemalaggning.DTOs.Employees;
using Schemalaggning.DTOs.LeaveRequests;

namespace Schemalaggning.DTOs.Me;

public class MeReadDto
{
    public UserAccountReadDto Account { get; set; } = new();

    public EmployeeReadDto? Employee { get; set; }

    public LeaveBalanceReadDto? LeaveBalance { get; set; }

    public List<MeShiftReadDto> UpcomingShifts { get; set; } = new();
}
