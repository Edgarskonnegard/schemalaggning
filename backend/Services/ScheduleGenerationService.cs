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

        var requestedDurationInDays = dto.PeriodEnd.DayNumber - dto.PeriodStart.DayNumber;
        var periodStart = await GetFirstAvailableStartDateAsync(
            dto.StoreId,
            dto.PeriodStart,
            requestedDurationInDays);
        var periodEnd = periodStart.AddDays(requestedDurationInDays);
        var cycleAnchorDate = await GetCycleAnchorDateAsync(dto.StoreId) ?? periodStart;

        if (cycleAnchorDate > periodStart)
        {
            cycleAnchorDate = periodStart;
        }

        var rules = (await _baseScheduleRuleRepository.GetAllWithDetailsAsync())
            .Where(rule => rule.Employee.StoreId == dto.StoreId)
            .ToList();
        var employeeIds = rules
            .Select(rule => rule.EmployeeId)
            .Distinct()
            .ToList();
        var blockedDates = await GetBlockedLeaveDatesAsync(employeeIds, periodStart, periodEnd);

        var schedule = new Schedule
        {
            Name = string.IsNullOrWhiteSpace(dto.Name)
                ? $"Schema {periodStart:yyyy-MM-dd} - {periodEnd:yyyy-MM-dd}"
                : dto.Name.Trim(),
            StoreId = dto.StoreId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = "Draft"
        };

        for (var date = periodStart; date <= periodEnd; date = date.AddDays(1))
        {
            var weekInCycle = GetWeekInCycle(cycleAnchorDate, date);

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
        if (createdWithShifts is null)
        {
            throw new InvalidOperationException("Created schedule could not be loaded.");
        }

        var readDto = createdWithShifts.ToReadDto();
        readDto.CoverageGaps = await GetCoverageGapsAsync(createdWithShifts);
        return readDto;
    }

    private async Task<DateOnly> GetFirstAvailableStartDateAsync(
        int storeId,
        DateOnly requestedStart,
        int durationInDays)
    {
        var publishedSchedules = await _context.Schedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.StoreId == storeId &&
                schedule.Status == "Published" &&
                schedule.PeriodEnd >= requestedStart)
            .OrderBy(schedule => schedule.PeriodStart)
            .ToListAsync();

        var start = requestedStart;

        foreach (var schedule in publishedSchedules)
        {
            var periodEnd = start.AddDays(durationInDays);

            if (schedule.PeriodStart > periodEnd)
            {
                break;
            }

            if (schedule.PeriodStart <= periodEnd && schedule.PeriodEnd >= start)
            {
                start = schedule.PeriodEnd.AddDays(1);
            }
        }

        return start;
    }

    private Task<DateOnly?> GetCycleAnchorDateAsync(int storeId)
    {
        return _context.Schedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.StoreId == storeId &&
                schedule.Status == "Published")
            .OrderBy(schedule => schedule.PeriodStart)
            .Select(schedule => (DateOnly?)schedule.PeriodStart)
            .FirstOrDefaultAsync();
    }

    private static int GetWeekInCycle(DateOnly cycleAnchorDate, DateOnly date)
    {
        var daysFromStart = date.DayNumber - cycleAnchorDate.DayNumber;
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

    private async Task<List<ScheduleCoverageGapDto>> GetCoverageGapsAsync(Schedule schedule)
    {
        var coverageRules = await _context.StoreCoverageRules
            .AsNoTracking()
            .Include(rule => rule.ShiftType)
            .Where(rule => rule.StoreId == schedule.StoreId)
            .ToListAsync();

        var gaps = new List<ScheduleCoverageGapDto>();

        for (var date = schedule.PeriodStart; date <= schedule.PeriodEnd; date = date.AddDays(1))
        {
            foreach (var rule in coverageRules.Where(rule => rule.DayOfWeek == date.DayOfWeek))
            {
                var assignedCount = schedule.Shifts.Count(shift =>
                    shift.Date == date &&
                    shift.ShiftTypeId == rule.ShiftTypeId &&
                    shift.StartTime == rule.StartTime &&
                    shift.EndTime == rule.EndTime);

                if (assignedCount >= rule.RequiredCount)
                {
                    continue;
                }

                gaps.Add(new ScheduleCoverageGapDto
                {
                    Date = date,
                    ShiftTypeId = rule.ShiftTypeId,
                    ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
                    StartTime = rule.StartTime,
                    EndTime = rule.EndTime,
                    RequiredCount = rule.RequiredCount,
                    AssignedCount = assignedCount,
                    MissingCount = rule.RequiredCount - assignedCount
                });
            }
        }

        return gaps
            .OrderBy(gap => gap.Date)
            .ThenBy(gap => gap.StartTime)
            .ThenBy(gap => gap.ShiftTypeName)
            .ToList();
    }
}
