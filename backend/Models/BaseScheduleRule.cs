namespace Schemalaggning.Models;

public class BaseScheduleRule
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
}