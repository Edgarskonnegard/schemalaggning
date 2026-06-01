namespace Schemalaggning.Models;

public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public ICollection<RoleShiftType> RoleShiftTypes { get; set; } = new List<RoleShiftType>();
}
