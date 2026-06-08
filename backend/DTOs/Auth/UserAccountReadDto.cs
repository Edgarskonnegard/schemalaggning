namespace Schemalaggning.DTOs.Auth;

public class UserAccountReadDto
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string AccessRole { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }

    public int? StoreId { get; set; }

    public bool IsActive { get; set; }
}
