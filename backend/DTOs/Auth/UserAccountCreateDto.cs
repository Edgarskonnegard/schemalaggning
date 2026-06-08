namespace Schemalaggning.DTOs.Auth;

public class UserAccountCreateDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string AccessRole { get; set; } = "Employee";

    public int? EmployeeId { get; set; }

    public int? StoreId { get; set; }
}
