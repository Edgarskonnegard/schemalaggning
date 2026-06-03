namespace Schemalaggning.DTOs.Employees;

public class EmployeeCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public int RoleId { get; set; }

    public decimal EmploymentPercentage { get; set; }
}
