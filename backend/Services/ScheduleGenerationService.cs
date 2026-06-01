using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private readonly IBaseScheduleRuleRepository _baseScheduleRuleRepository;
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleGenerationService(
        IBaseScheduleRuleRepository baseScheduleRuleRepository,
        IScheduleRepository scheduleRepository)
    {
        _baseScheduleRuleRepository = baseScheduleRuleRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<ScheduleReadDto> GenerateFromBaseScheduleAsync(ScheduleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Schedule name is required.");
        }

        if (dto.PeriodEnd < dto.PeriodStart)
        {
            throw new ArgumentException("Period end must be after or equal to period start.");
        }

        var rules = await _baseScheduleRuleRepository.GetAllWithDetailsAsync();
        var schedule = new Schedule
        {
            Name = dto.Name.Trim(),
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Status = "Draft"
        };

        for (var date = dto.PeriodStart; date <= dto.PeriodEnd; date = date.AddDays(1))
        {
            foreach (var rule in rules.Where(rule => rule.DayOfWeek == date.DayOfWeek))
            {
                schedule.Shifts.Add(new Shift
                {
                    EmployeeId = rule.EmployeeId,
                    ShiftTypeId = rule.ShiftTypeId,
                    Date = date,
                    StartTime = rule.ShiftType.DefaultStartTime,
                    EndTime = rule.ShiftType.DefaultEndTime,
                    Source = "BaseSchedule",
                    Status = "Draft"
                });
            }
        }

        var created = await _scheduleRepository.CreateAsync(schedule);
        var createdWithShifts = await _scheduleRepository.GetByIdWithShiftsAsync(created.Id);
        return createdWithShifts!.ToReadDto();
    }
}
