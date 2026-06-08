namespace Schemalaggning.DTOs.ScheduleGenerationSettings;

public class ScheduleGenerationSettingsUpdateDto
{
    public decimal MinimumRestHours { get; set; }

    public int MaxConsecutiveWorkDays { get; set; }

    public bool BalanceWeekends { get; set; }
}
