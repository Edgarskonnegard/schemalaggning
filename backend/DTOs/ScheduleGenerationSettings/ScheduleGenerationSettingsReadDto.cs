namespace Schemalaggning.DTOs.ScheduleGenerationSettings;

public class ScheduleGenerationSettingsReadDto
{
    public int StoreId { get; set; }

    public decimal MinimumRestHours { get; set; }

    public int MaxConsecutiveWorkDays { get; set; }

    public bool BalanceWeekends { get; set; }
}
