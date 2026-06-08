using Schemalaggning.DTOs.Auth;
using Schemalaggning.DTOs.Employees;

namespace Schemalaggning.DTOs.Me;

public class MeReadDto
{
    public UserAccountReadDto Account { get; set; } = new();

    public EmployeeReadDto? Employee { get; set; }

    public List<MeShiftReadDto> UpcomingShifts { get; set; } = new();
}
