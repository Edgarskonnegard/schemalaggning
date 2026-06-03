namespace Schemalaggning.DTOs.ShiftTypes;

public class ShiftTypeReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<int> RoleIds { get; set; } = new();

    public List<string> RoleNames { get; set; } = new();

    public TimeOnly DefaultStartTime { get; set; }

    public TimeOnly DefaultEndTime { get; set; }
}
