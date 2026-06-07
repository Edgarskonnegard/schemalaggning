using Schemalaggning.DTOs.BaseScheduleGeneration;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Schemalaggning.Services;

public class BaseScheduleGenerationService : IBaseScheduleGenerationService
{
    private const decimal FullTimeWeeklyHours = 40m;
    private const int WeeksInCycle = 4;

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IStoreCoverageRuleRepository _coverageRuleRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly AppDbContext _context;

    public BaseScheduleGenerationService(
        IEmployeeRepository employeeRepository,
        IStoreCoverageRuleRepository coverageRuleRepository,
        IStoreRepository storeRepository,
        AppDbContext context)
    {
        _employeeRepository = employeeRepository;
        _coverageRuleRepository = coverageRuleRepository;
        _storeRepository = storeRepository;
        _context = context;
    }

    public async Task<BaseScheduleGenerationResultDto> GenerateForStoreAsync(int storeId)
    {
        if (!await _storeRepository.ExistsAsync(storeId))
        {
            throw new InvalidOperationException($"Store {storeId} does not exist.");
        }

        var employees = await _employeeRepository.GetByStoreIdAsync(storeId);
        var coverageRules = await _coverageRuleRepository.GetByStoreIdAsync(storeId);
        var settings = await _context.ScheduleGenerationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.StoreId == storeId);
        var minimumRestHours =
            settings?.MinimumRestHours ?? ScheduleGenerationSettingsService.DefaultMinimumRestHours;
        var maxConsecutiveWorkDays =
            settings?.MaxConsecutiveWorkDays ?? ScheduleGenerationSettingsService.DefaultMaxConsecutiveWorkDays;
        var balanceWeekends =
            settings?.BalanceWeekends ?? ScheduleGenerationSettingsService.DefaultBalanceWeekends;

        var result = new BaseScheduleGenerationResultDto
        {
            StoreId = storeId,
            EmployeeCount = employees.Count,
            CoverageRuleCount = coverageRules.Count
        };

        if (employees.Count == 0)
        {
            result.Warnings.Add("Butiken har inga anställda att fördela pass till.");
            return result;
        }

        if (coverageRules.Count == 0)
        {
            result.Warnings.Add("Butiken har inget bemanningsbehov att generera från.");
            return result;
        }

        var canWorkCache = await BuildCanWorkCacheAsync(employees, coverageRules);
        var generatedRules = new List<BaseScheduleDraftRule>();
        var unassignedRules = new List<BaseScheduleUnassignedDraftRule>();
        var employeeHours = employees.ToDictionary(employee => employee.Id, _ => 0m);
        var occupiedDays = employees.ToDictionary(
            employee => employee.Id,
            _ => new HashSet<(int WeekInCycle, DayOfWeek DayOfWeek)>());
        var assignedShifts = employees.ToDictionary(
            employee => employee.Id,
            _ => new List<AssignedShiftWindow>());
        var occupiedWeekends = employees.ToDictionary(employee => employee.Id, _ => new HashSet<int>());

        foreach (var week in Enumerable.Range(1, WeeksInCycle))
        {
            foreach (var need in ExpandNeeds(coverageRules))
            {
                var employee = PickEmployee(
                    employees,
                    need,
                    week,
                    employeeHours,
                    occupiedDays,
                    assignedShifts,
                    occupiedWeekends,
                    canWorkCache,
                    minimumRestHours,
                    maxConsecutiveWorkDays,
                    balanceWeekends);

                if (employee is null)
                {
                    result.UnassignedNeedCount++;
                    unassignedRules.Add(new BaseScheduleUnassignedDraftRule
                    {
                        ShiftTypeId = need.ShiftTypeId,
                        WeekInCycle = week,
                        DayOfWeek = need.DayOfWeek,
                        StartTime = need.StartTime,
                        EndTime = need.EndTime
                    });
                    continue;
                }

                generatedRules.Add(new BaseScheduleDraftRule
                {
                    EmployeeId = employee.Id,
                    ShiftTypeId = need.ShiftTypeId,
                    WeekInCycle = week,
                    DayOfWeek = need.DayOfWeek,
                    StartTime = need.StartTime,
                    EndTime = need.EndTime
                });

                employeeHours[employee.Id] += GetHours(need.StartTime, need.EndTime);
                occupiedDays[employee.Id].Add((week, need.DayOfWeek));
                assignedShifts[employee.Id].Add(new AssignedShiftWindow(
                    week,
                    need.DayOfWeek,
                    need.StartTime,
                    need.EndTime));

                if (IsWeekend(need.DayOfWeek))
                {
                    occupiedWeekends[employee.Id].Add(week);
                }
            }
        }

        result.CreatedRuleCount = generatedRules.Count;

        if (result.UnassignedNeedCount > 0)
        {
            result.Warnings.Add($"{result.UnassignedNeedCount} behov kunde inte placeras eftersom ingen ledig anställd matchade roll/passtyp och schemaregler.");
        }

        var batch = new BaseScheduleGenerationBatch
        {
            StoreId = storeId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            WarningText = string.Join('\n', result.Warnings),
            DraftRules = generatedRules,
            UnassignedDraftRules = unassignedRules,
            EmployeeApprovals = employees
                .Select(employee => new BaseScheduleEmployeeApproval
                {
                    EmployeeId = employee.Id,
                    Status = "Pending"
                })
                .ToList()
        };

        _context.BaseScheduleGenerationBatches.Add(batch);
        await _context.SaveChangesAsync();

        result.BatchId = batch.Id;
        result.Status = batch.Status;
        return result;
    }

    private async Task<Dictionary<(int EmployeeId, int ShiftTypeId), bool>> BuildCanWorkCacheAsync(
        List<Employee> employees,
        List<StoreCoverageRule> coverageRules)
    {
        var cache = new Dictionary<(int EmployeeId, int ShiftTypeId), bool>();
        var shiftTypeIds = coverageRules
            .Select(rule => rule.ShiftTypeId)
            .Distinct()
            .ToList();

        foreach (var employee in employees)
        {
            foreach (var shiftTypeId in shiftTypeIds)
            {
                cache[(employee.Id, shiftTypeId)] =
                    await _employeeRepository.CanWorkShiftTypeAsync(employee.Id, shiftTypeId);
            }
        }

        return cache;
    }

    private static List<StoreCoverageRule> ExpandNeeds(List<StoreCoverageRule> coverageRules)
    {
        return coverageRules
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .SelectMany(rule => Enumerable.Repeat(rule, rule.RequiredCount))
            .ToList();
    }

    private static Employee? PickEmployee(
        List<Employee> employees,
        StoreCoverageRule need,
        int weekInCycle,
        Dictionary<int, decimal> employeeHours,
        Dictionary<int, HashSet<(int WeekInCycle, DayOfWeek DayOfWeek)>> occupiedDays,
        Dictionary<int, List<AssignedShiftWindow>> assignedShifts,
        Dictionary<int, HashSet<int>> occupiedWeekends,
        Dictionary<(int EmployeeId, int ShiftTypeId), bool> canWorkCache,
        decimal minimumRestHours,
        int maxConsecutiveWorkDays,
        bool balanceWeekends)
    {
        var availableEmployees = employees
            .Where(employee => canWorkCache.GetValueOrDefault((employee.Id, need.ShiftTypeId)))
            .Where(employee => !occupiedDays[employee.Id].Contains((weekInCycle, need.DayOfWeek)))
            .Where(employee => HasMaxConsecutiveWorkDays(
                occupiedDays[employee.Id],
                weekInCycle,
                need.DayOfWeek,
                maxConsecutiveWorkDays))
            .Where(employee => HasWeekendRest(
                occupiedWeekends[employee.Id],
                weekInCycle,
                need.DayOfWeek,
                balanceWeekends))
            .Where(employee => HasMinimumRest(
                assignedShifts[employee.Id],
                weekInCycle,
                need.DayOfWeek,
                need.StartTime,
                need.EndTime,
                minimumRestHours))
            .ToList();

        if (availableEmployees.Count == 0)
        {
            return null;
        }

        var hours = GetHours(need.StartTime, need.EndTime);
        var underTarget = availableEmployees
            .Where(employee => employeeHours[employee.Id] + hours <= GetTargetCycleHours(employee) + 0.25m)
            .ToList();

        var candidates = underTarget.Count > 0 ? underTarget : availableEmployees;
        return candidates
            .OrderBy(employee => GetLoadRatio(employee, employeeHours[employee.Id]))
            .ThenBy(_ => Random.Shared.Next())
            .First();
    }

    private static decimal GetLoadRatio(Employee employee, decimal currentHours)
    {
        var targetHours = GetTargetCycleHours(employee);
        return targetHours <= 0 ? currentHours + 1000m : currentHours / targetHours;
    }

    private static decimal GetTargetCycleHours(Employee employee)
    {
        return employee.EmploymentPercentage / 100m * FullTimeWeeklyHours * WeeksInCycle;
    }

    private static decimal GetHours(TimeOnly startTime, TimeOnly endTime)
    {
        var hours = (decimal)(endTime - startTime).TotalHours;
        return hours > 0 ? hours : 0;
    }

    private static bool HasMinimumRest(
        List<AssignedShiftWindow> assignedShifts,
        int weekInCycle,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal minimumRestHours)
    {
        var candidate = new AssignedShiftWindow(weekInCycle, dayOfWeek, startTime, endTime);
        var minimumRestMinutes = (int)(minimumRestHours * 60m);
        const int cycleMinutes = WeeksInCycle * 7 * 24 * 60;

        return assignedShifts.All(assignedShift =>
            HasRestBetweenWindows(candidate, assignedShift, minimumRestMinutes) &&
            HasRestBetweenWindows(candidate with { MinuteOffset = -cycleMinutes }, assignedShift, minimumRestMinutes) &&
            HasRestBetweenWindows(candidate with { MinuteOffset = cycleMinutes }, assignedShift, minimumRestMinutes));
    }

    private static bool HasMaxConsecutiveWorkDays(
        HashSet<(int WeekInCycle, DayOfWeek DayOfWeek)> occupiedDays,
        int candidateWeekInCycle,
        DayOfWeek candidateDayOfWeek,
        int maxConsecutiveWorkDays)
    {
        var workDays = new bool[WeeksInCycle * 7];
        foreach (var occupiedDay in occupiedDays)
        {
            workDays[GetCycleDayIndex(occupiedDay.WeekInCycle, occupiedDay.DayOfWeek)] = true;
        }

        workDays[GetCycleDayIndex(candidateWeekInCycle, candidateDayOfWeek)] = true;
        return GetLongestConsecutiveWorkDays(workDays) <= maxConsecutiveWorkDays;
    }

    private static bool HasWeekendRest(
        HashSet<int> occupiedWeekends,
        int candidateWeekInCycle,
        DayOfWeek candidateDayOfWeek,
        bool balanceWeekends)
    {
        if (!balanceWeekends || !IsWeekend(candidateDayOfWeek))
        {
            return true;
        }

        var previousWeekend = candidateWeekInCycle == 1 ? WeeksInCycle : candidateWeekInCycle - 1;
        var nextWeekend = candidateWeekInCycle == WeeksInCycle ? 1 : candidateWeekInCycle + 1;

        return !occupiedWeekends.Contains(previousWeekend) &&
            !occupiedWeekends.Contains(nextWeekend);
    }

    private static int GetLongestConsecutiveWorkDays(bool[] workDays)
    {
        var longestRun = 0;

        for (var start = 0; start < workDays.Length; start++)
        {
            var currentRun = 0;

            for (var offset = 0; offset < workDays.Length; offset++)
            {
                if (!workDays[(start + offset) % workDays.Length])
                {
                    break;
                }

                currentRun++;
            }

            longestRun = Math.Max(longestRun, currentRun);
        }

        return longestRun;
    }

    private static bool IsWeekend(DayOfWeek dayOfWeek)
    {
        return dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    private static int GetCycleDayIndex(int weekInCycle, DayOfWeek dayOfWeek)
    {
        var dayIndex = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
        return (weekInCycle - 1) * 7 + dayIndex;
    }

    private static bool HasRestBetweenWindows(
        AssignedShiftWindow first,
        AssignedShiftWindow second,
        int minimumRestMinutes)
    {
        return Math.Abs(first.StartMinute - second.EndMinute) >= minimumRestMinutes &&
            Math.Abs(second.StartMinute - first.EndMinute) >= minimumRestMinutes;
    }

    private sealed record AssignedShiftWindow(
        int WeekInCycle,
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime)
    {
        public int MinuteOffset { get; init; }

        public int StartMinute => GetAbsoluteMinute(WeekInCycle, DayOfWeek, StartTime) + MinuteOffset;

        public int EndMinute
        {
            get
            {
                var endMinute = GetAbsoluteMinute(WeekInCycle, DayOfWeek, EndTime);
                return endMinute <= StartMinute ? endMinute + 24 * 60 : endMinute;
            }
        }

        private static int GetAbsoluteMinute(int weekInCycle, DayOfWeek dayOfWeek, TimeOnly time)
        {
            return GetCycleDayIndex(weekInCycle, dayOfWeek) * 24 * 60 + time.Hour * 60 + time.Minute;
        }
    }
}
