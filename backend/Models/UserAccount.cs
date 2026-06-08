namespace Schemalaggning.Models;

public class UserAccount
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string AccessRole { get; set; } = "Employee";

    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public int? StoreId { get; set; }

    public Store? Store { get; set; }

    public bool IsActive { get; set; } = true;
}
