using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
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
    private readonly AppDbContext _context;

    public ScheduleGenerationService(
        IBaseScheduleRuleRepository baseScheduleRuleRepository,
        IScheduleRepository scheduleRepository,
        IStoreRepository storeRepository,
        AppDbContext context)
    {
        _baseScheduleRuleRepository = baseScheduleRuleRepository;
        _scheduleRepository = scheduleRepository;
        _storeRepository = storeRepository;
        _context = context;
    }

    public async Task<ScheduleReadDto> GenerateFromBaseScheduleAsync(ScheduleCreateDto dto)
    {
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
        var employeeIds = rules
            .Select(rule => rule.EmployeeId)
            .Distinct()
            .ToList();
        var blockedDates = await GetBlockedLeaveDatesAsync(employeeIds, dto.PeriodStart, dto.PeriodEnd);

        var schedule = new Schedule
        {
            Name = string.IsNullOrWhiteSpace(dto.Name)
                ? $"Schema {dto.PeriodStart:yyyy-MM-dd} - {dto.PeriodEnd:yyyy-MM-dd}"
                : dto.Name.Trim(),
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
                if (blockedDates.Contains((rule.EmployeeId, date)))
                {
                    continue;
                }

                schedule.Shifts.Add(new Shift
                {
                    EmployeeId = rule.EmployeeId,
                    ShiftTypeId = rule.ShiftTypeId,
                    Date = date,
                    StartTime = rule.StartTime,
                    EndTime = rule.EndTime,
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

    private async Task<HashSet<(int EmployeeId, DateOnly Date)>> GetBlockedLeaveDatesAsync(
        List<int> employeeIds,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var leaveRequests = await _context.LeaveRequests
            .AsNoTracking()
            .Where(request =>
                employeeIds.Contains(request.EmployeeId) &&
                (request.Status == "Pending" || request.Status == "Approved") &&
                request.StartDate <= periodEnd &&
                request.EndDate >= periodStart)
            .ToListAsync();

        var blockedDates = new HashSet<(int EmployeeId, DateOnly Date)>();

        foreach (var request in leaveRequests)
        {
            var start = request.StartDate < periodStart ? periodStart : request.StartDate;
            var end = request.EndDate > periodEnd ? periodEnd : request.EndDate;

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                blockedDates.Add((request.EmployeeId, date));
            }
        }

        return blockedDates;
    }
}
