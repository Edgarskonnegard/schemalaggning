using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private readonly IBaseScheduleRuleRepository _baseScheduleRuleRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IStoreRepository _storeRepository;

    public ScheduleGenerationService(
        IBaseScheduleRuleRepository baseScheduleRuleRepository,
        IScheduleRepository scheduleRepository,
        IStoreRepository storeRepository)
    {
        _baseScheduleRuleRepository = baseScheduleRuleRepository;
        _scheduleRepository = scheduleRepository;
        _storeRepository = storeRepository;
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

        if (!await _storeRepository.ExistsAsync(dto.StoreId))
        {
            throw new InvalidOperationException($"Store {dto.StoreId} does not exist.");
        }

        var rules = (await _baseScheduleRuleRepository.GetAllWithDetailsAsync())
            .Where(rule => rule.Employee.StoreId == dto.StoreId)
            .ToList();

        var schedule = new Schedule
        {
            Name = dto.Name.Trim(),
            StoreId = dto.StoreId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Status = "Draft"
        };

        for (var date = dto.PeriodStart; date <= dto.PeriodEnd; date = date.AddDays(1))
        {
            var weekInCycle = GetWeekInCycle(dto.PeriodStart, date);

            foreach (var rule in rules.Where(rule =>
                rule.WeekInCycle == weekInCycle &&
                rule.DayOfWeek == date.DayOfWeek))
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

    private static int GetWeekInCycle(DateOnly periodStart, DateOnly date)
    {
        var daysFromStart = date.DayNumber - periodStart.DayNumber;
        var weekIndex = daysFromStart / 7;
        return weekIndex % 4 + 1;
    }
}
