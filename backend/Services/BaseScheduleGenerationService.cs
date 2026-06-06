using Schemalaggning.DTOs.BaseScheduleGeneration;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

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
                    canWorkCache);

                if (employee is null)
                {
                    result.UnassignedNeedCount++;
                    unassignedRules.Add(new BaseScheduleUnassignedDraftRule
                    {
                        ShiftTypeId = need.ShiftTypeId,
                        WeekInCycle = week,
                        DayOfWeek = need.DayOfWeek
                    });
                    continue;
                }

                generatedRules.Add(new BaseScheduleDraftRule
                {
                    EmployeeId = employee.Id,
                    ShiftTypeId = need.ShiftTypeId,
                    WeekInCycle = week,
                    DayOfWeek = need.DayOfWeek
                });

                employeeHours[employee.Id] += GetHours(need.StartTime, need.EndTime);
                occupiedDays[employee.Id].Add((week, need.DayOfWeek));
            }
        }

        result.CreatedRuleCount = generatedRules.Count;

        if (result.UnassignedNeedCount > 0)
        {
            result.Warnings.Add($"{result.UnassignedNeedCount} behov kunde inte placeras eftersom ingen ledig anställd matchade roll/passtyp den dagen.");
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
        Dictionary<(int EmployeeId, int ShiftTypeId), bool> canWorkCache)
    {
        var availableEmployees = employees
            .Where(employee => canWorkCache.GetValueOrDefault((employee.Id, need.ShiftTypeId)))
            .Where(employee => !occupiedDays[employee.Id].Contains((weekInCycle, need.DayOfWeek)))
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
}
