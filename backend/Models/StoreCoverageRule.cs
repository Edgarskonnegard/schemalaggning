namespace Schemalaggning.Models;

public class StoreCoverageRule
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public int ShiftTypeId { get; set; }

    public ShiftType ShiftType { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public int RequiredCount { get; set; } = 1;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
