namespace Schemalaggning.DTOs.ShiftTypes;

public class ShiftTypeCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }
}