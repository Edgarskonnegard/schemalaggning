namespace Schemalaggning.Models;

public class EmployeeShiftType
{
    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;
}