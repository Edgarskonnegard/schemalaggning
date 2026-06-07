namespace Schemalaggning.Models;

public class ScheduleGenerationSettings
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public decimal MinimumRestHours { get; set; } = 11m;

    public int MaxConsecutiveWorkDays { get; set; } = 5;

    public bool BalanceWeekends { get; set; } = true;
}
