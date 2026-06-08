namespace Schemalaggning.DTOs.Employees;

public class EmployeeCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public int RoleId { get; set; }

    public decimal EmploymentPercentage { get; set; }

    public string? AccountEmail { get; set; }

    public string? AccountPassword { get; set; }

    public string AccountAccessRole { get; set; } = "Employee";

    public bool AccountIsActive { get; set; } = true;
}
