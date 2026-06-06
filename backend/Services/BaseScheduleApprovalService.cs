using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.BaseScheduleApprovals;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class BaseScheduleApprovalService : IBaseScheduleApprovalService
{
    private const decimal FullTimeWeeklyHours = 40m;

    private readonly AppDbContext _context;
    private readonly IBaseScheduleRuleRepository _baseScheduleRuleRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public BaseScheduleApprovalService(
        AppDbContext context,
        IBaseScheduleRuleRepository baseScheduleRuleRepository,
        IEmployeeRepository employeeRepository)
    {
        _context = context;
        _baseScheduleRuleRepository = baseScheduleRuleRepository;
        _employeeRepository = employeeRepository;
    }

    public Task<int> GetPendingCountAsync()
    {
        return _context.BaseScheduleEmployeeApprovals
            .CountAsync(approval => approval.Status == "Pending");
    }

    public async Task<List<BaseScheduleApprovalBatchReadDto>> GetPendingAsync()
    {
        var batches = await QueryBatches()
            .Where(batch => batch.EmployeeApprovals.Any(approval => approval.Status == "Pending"))
            .OrderBy(batch => batch.CreatedAt)
            .ToListAsync();

        return batches.Select(ToReadDto).ToList();
    }

    public async Task<BaseScheduleApprovalBatchReadDto?> GetByIdAsync(int id)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == id);

        return batch is null ? null : ToReadDto(batch);
    }

    public async Task<bool> ApproveAsync(int id)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == id && batch.Status == "Pending");

        if (batch is null)
        {
            return false;
        }

        var employeeIds = (await _employeeRepository.GetByStoreIdAsync(batch.StoreId))
            .Select(employee => employee.Id)
            .ToList();

        await _baseScheduleRuleRepository.DeleteByEmployeeIdsAsync(employeeIds);

        var rules = batch.DraftRules.Select(rule => new BaseScheduleRule
        {
            EmployeeId = rule.EmployeeId,
            ShiftTypeId = rule.ShiftTypeId,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek
        }).ToList();

        if (rules.Count > 0)
        {
            await _baseScheduleRuleRepository.AddRangeAsync(rules);
        }

        batch.Status = "Approved";
        batch.ApprovedAt = DateTime.UtcNow;

        foreach (var approval in batch.EmployeeApprovals.Where(approval => approval.Status == "Pending"))
        {
            approval.Status = "Approved";
            approval.ApprovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectAsync(int id)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == id && batch.Status == "Pending");

        if (batch is null)
        {
            return false;
        }

        batch.Status = "Rejected";
        batch.RejectedAt = DateTime.UtcNow;

        foreach (var approval in batch.EmployeeApprovals.Where(approval => approval.Status == "Pending"))
        {
            approval.Status = "Rejected";
            approval.RejectedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ApproveEmployeeAsync(int batchId, int employeeId)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == batchId && batch.Status == "Pending");

        if (batch is null)
        {
            return false;
        }

        var approval = batch.EmployeeApprovals.FirstOrDefault(approval =>
            approval.EmployeeId == employeeId &&
            approval.Status == "Pending");

        if (approval is null)
        {
            return false;
        }

        await _baseScheduleRuleRepository.DeleteByEmployeeIdsAsync(new List<int> { employeeId });

        var rules = batch.DraftRules
            .Where(rule => rule.EmployeeId == employeeId)
            .Select(rule => new BaseScheduleRule
            {
                EmployeeId = rule.EmployeeId,
                ShiftTypeId = rule.ShiftTypeId,
                WeekInCycle = rule.WeekInCycle,
                DayOfWeek = rule.DayOfWeek
            })
            .ToList();

        if (rules.Count > 0)
        {
            await _baseScheduleRuleRepository.AddRangeAsync(rules);
        }

        approval.Status = "Approved";
        approval.ApprovedAt = DateTime.UtcNow;
        CompleteBatchIfDone(batch);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectEmployeeAsync(int batchId, int employeeId)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == batchId && batch.Status == "Pending");

        if (batch is null)
        {
            return false;
        }

        var approval = batch.EmployeeApprovals.FirstOrDefault(approval =>
            approval.EmployeeId == employeeId &&
            approval.Status == "Pending");

        if (approval is null)
        {
            return false;
        }

        approval.Status = "Rejected";
        approval.RejectedAt = DateTime.UtcNow;
        CompleteBatchIfDone(batch);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<BaseScheduleDraftRuleReadDto?> AddDraftRuleAsync(
        int batchId,
        int employeeId,
        BaseScheduleDraftRuleCreateDto dto)
    {
        var batch = await QueryBatches()
            .FirstOrDefaultAsync(batch => batch.Id == batchId && batch.Status == "Pending");

        if (batch is null)
        {
            return null;
        }

        var approval = batch.EmployeeApprovals.FirstOrDefault(approval =>
            approval.EmployeeId == employeeId &&
            approval.Status == "Pending");

        if (approval is null)
        {
            return null;
        }

        var unassignedRule = batch.UnassignedDraftRules.FirstOrDefault(rule =>
            rule.Id == dto.UnassignedRuleId);

        if (unassignedRule is null)
        {
            return null;
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(employeeId, unassignedRule.ShiftTypeId))
        {
            throw new InvalidOperationException("Employee role does not allow this shift type.");
        }

        var hasRuleOnDay = batch.DraftRules.Any(rule =>
            rule.EmployeeId == employeeId &&
            rule.WeekInCycle == unassignedRule.WeekInCycle &&
            rule.DayOfWeek == unassignedRule.DayOfWeek);

        if (hasRuleOnDay)
        {
            throw new InvalidOperationException("Employee already has a draft rule on this week and day.");
        }

        var rule = new BaseScheduleDraftRule
        {
            BatchId = batchId,
            EmployeeId = employeeId,
            ShiftTypeId = unassignedRule.ShiftTypeId,
            WeekInCycle = unassignedRule.WeekInCycle,
            DayOfWeek = unassignedRule.DayOfWeek
        };

        _context.BaseScheduleDraftRules.Add(rule);
        _context.BaseScheduleUnassignedDraftRules.Remove(unassignedRule);
        await _context.SaveChangesAsync();

        var createdRuleId = rule.Id;
        var created = await _context.BaseScheduleDraftRules
            .Include(rule => rule.Employee)
            .Include(rule => rule.ShiftType)
            .FirstAsync(rule => rule.Id == createdRuleId);

        return ToDraftRuleDto(created);
    }

    public async Task<bool> DeleteDraftRuleAsync(int batchId, int employeeId, int ruleId)
    {
        var isPending = await _context.BaseScheduleEmployeeApprovals.AnyAsync(approval =>
            approval.BatchId == batchId &&
            approval.EmployeeId == employeeId &&
            approval.Status == "Pending" &&
            approval.Batch.Status == "Pending");

        if (!isPending)
        {
            return false;
        }

        var rule = await _context.BaseScheduleDraftRules.FirstOrDefaultAsync(rule =>
            rule.Id == ruleId &&
            rule.BatchId == batchId &&
            rule.EmployeeId == employeeId);

        if (rule is null)
        {
            return false;
        }

        _context.BaseScheduleUnassignedDraftRules.Add(new BaseScheduleUnassignedDraftRule
        {
            BatchId = batchId,
            ShiftTypeId = rule.ShiftTypeId,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek
        });
        _context.BaseScheduleDraftRules.Remove(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    private IQueryable<BaseScheduleGenerationBatch> QueryBatches()
    {
        return _context.BaseScheduleGenerationBatches
            .Include(batch => batch.Store)
            .Include(batch => batch.DraftRules)
                .ThenInclude(rule => rule.Employee)
            .Include(batch => batch.DraftRules)
                .ThenInclude(rule => rule.ShiftType)
            .Include(batch => batch.EmployeeApprovals)
                .ThenInclude(approval => approval.Employee)
            .Include(batch => batch.UnassignedDraftRules)
                .ThenInclude(rule => rule.ShiftType);
    }

    private static BaseScheduleApprovalBatchReadDto ToReadDto(BaseScheduleGenerationBatch batch)
    {
        var draftRules = batch.DraftRules
            .OrderBy(rule => rule.WeekInCycle)
            .ThenBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.Employee.Name)
            .Select(ToDraftRuleDto)
            .ToList();

        return new BaseScheduleApprovalBatchReadDto
        {
            Id = batch.Id,
            StoreId = batch.StoreId,
            StoreName = batch.Store?.Name ?? string.Empty,
            Status = batch.Status,
            CreatedAt = batch.CreatedAt,
            ApprovedAt = batch.ApprovedAt,
            RejectedAt = batch.RejectedAt,
            DraftRuleCount = draftRules.Count,
            Warnings = GetWarnings(batch.WarningText),
            DraftRules = draftRules,
            EmployeeSummaries = GetEmployeeSummaries(draftRules, batch.EmployeeApprovals.ToList()),
            EmployeeApprovals = batch.EmployeeApprovals
                .OrderBy(approval => approval.Employee.Name)
                .Select(ToEmployeeApprovalDto)
                .ToList(),
            UnassignedDraftRules = batch.UnassignedDraftRules
                .OrderBy(rule => rule.WeekInCycle)
                .ThenBy(rule => rule.DayOfWeek)
                .ThenBy(rule => rule.ShiftType.Name)
                .Select(ToUnassignedRuleDto)
                .ToList()
        };
    }

    private static BaseScheduleDraftRuleReadDto ToDraftRuleDto(BaseScheduleDraftRule rule)
    {
        return new BaseScheduleDraftRuleReadDto
        {
            Id = rule.Id,
            EmployeeId = rule.EmployeeId,
            EmployeeName = rule.Employee?.Name ?? string.Empty,
            EmploymentPercentage = rule.Employee?.EmploymentPercentage ?? 0,
            ShiftTypeId = rule.ShiftTypeId,
            ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek,
            StartTime = rule.ShiftType?.DefaultStartTime ?? default,
            EndTime = rule.ShiftType?.DefaultEndTime ?? default,
            Hours = GetHours(rule.ShiftType?.DefaultStartTime ?? default, rule.ShiftType?.DefaultEndTime ?? default)
        };
    }

    private static BaseScheduleEmployeeApprovalReadDto ToEmployeeApprovalDto(BaseScheduleEmployeeApproval approval)
    {
        return new BaseScheduleEmployeeApprovalReadDto
        {
            Id = approval.Id,
            EmployeeId = approval.EmployeeId,
            EmployeeName = approval.Employee?.Name ?? string.Empty,
            EmploymentPercentage = approval.Employee?.EmploymentPercentage ?? 0,
            Status = approval.Status,
            ApprovedAt = approval.ApprovedAt,
            RejectedAt = approval.RejectedAt
        };
    }

    private static BaseScheduleUnassignedDraftRuleReadDto ToUnassignedRuleDto(BaseScheduleUnassignedDraftRule rule)
    {
        return new BaseScheduleUnassignedDraftRuleReadDto
        {
            Id = rule.Id,
            ShiftTypeId = rule.ShiftTypeId,
            ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek,
            StartTime = rule.ShiftType?.DefaultStartTime ?? default,
            EndTime = rule.ShiftType?.DefaultEndTime ?? default,
            Hours = GetHours(rule.ShiftType?.DefaultStartTime ?? default, rule.ShiftType?.DefaultEndTime ?? default)
        };
    }

    private static List<BaseScheduleEmployeeSummaryDto> GetEmployeeSummaries(
        List<BaseScheduleDraftRuleReadDto> rules,
        List<BaseScheduleEmployeeApproval> approvals)
    {
        return approvals
            .OrderBy(approval => approval.Employee.Name)
            .Select(approval =>
            {
                var employeeRules = rules
                    .Where(rule => rule.EmployeeId == approval.EmployeeId)
                    .ToList();

                var weeklyHours = Enumerable.Range(1, 4)
                    .Select(week => employeeRules
                        .Where(rule => rule.WeekInCycle == week)
                        .Sum(rule => rule.Hours))
                    .ToList();

                return new BaseScheduleEmployeeSummaryDto
                {
                    EmployeeId = approval.EmployeeId,
                    EmployeeName = approval.Employee?.Name ?? string.Empty,
                    EmploymentPercentage = approval.Employee?.EmploymentPercentage ?? 0,
                    TargetWeeklyHours = (approval.Employee?.EmploymentPercentage ?? 0) / 100m * FullTimeWeeklyHours,
                    WeeklyHours = weeklyHours,
                    TotalHours = weeklyHours.Sum()
                };
            })
            .ToList();
    }

    private static void CompleteBatchIfDone(BaseScheduleGenerationBatch batch)
    {
        if (batch.EmployeeApprovals.Any(approval => approval.Status == "Pending"))
        {
            return;
        }

        var hasRejected = batch.EmployeeApprovals.Any(approval => approval.Status == "Rejected");
        batch.Status = hasRejected ? "Completed" : "Approved";
        batch.ApprovedAt = hasRejected ? null : DateTime.UtcNow;
        batch.RejectedAt = hasRejected ? DateTime.UtcNow : null;
    }

    private static List<string> GetWarnings(string warningText)
    {
        return warningText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static decimal GetHours(TimeOnly startTime, TimeOnly endTime)
    {
        var hours = (decimal)(endTime - startTime).TotalHours;
        return hours > 0 ? hours : 0;
    }
}
