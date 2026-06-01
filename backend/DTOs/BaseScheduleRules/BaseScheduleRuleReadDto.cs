namespace Schemalaggning.DTOs.BaseScheduleRules;

public class BaseScheduleRuleReadDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int ShiftTypeId { get; set; }

    public string ShiftTypeName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}