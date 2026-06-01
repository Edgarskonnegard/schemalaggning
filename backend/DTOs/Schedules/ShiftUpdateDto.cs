namespace Schemalaggning.DTOs.Schedules;

public class ShiftUpdateDto
{
    public int EmployeeId { get; set; }

    public int ShiftTypeId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}