namespace Schemalaggning.DTOs.ShiftTypes;

public class ShiftTypeCreateDto
{
    public string Name { get; set; } = string.Empty;

    public List<int> RoleIds { get; set; } = new();

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }
}
