namespace Schemalaggning.DTOs.Employees;

using Schemalaggning.DTOs.BaseScheduleRules;

public class EmployeeReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public decimal EmploymentPercentage { get; set; }

    public int? AccountId { get; set; }

    public string AccountEmail { get; set; } = string.Empty;

    public string AccountAccessRole { get; set; } = string.Empty;

    public bool AccountIsActive { get; set; }

    public List<BaseScheduleRuleReadDto> BaseSchedule { get; set; } = new();
}
