namespace Schemalaggning.DTOs.Employees;

public class EmployeeUpdateDto
{
    public string Name { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public decimal EmploymentPercentage { get; set; }
}