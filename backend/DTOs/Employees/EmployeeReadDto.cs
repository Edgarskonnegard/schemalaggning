namespace Schemalaggning.DTOs.Employees;

using Schemalaggning.DTOs.BaseScheduleRules;

public class EmployeeReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public decimal EmploymentPercentage { get; set; }

    public List<BaseScheduleRuleReadDto> BaseSchedule { get; set; } = new();
}
