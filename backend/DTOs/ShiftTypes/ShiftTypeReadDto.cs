namespace Schemalaggning.DTOs.ShiftTypes;

public class ShiftTypeReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }
}