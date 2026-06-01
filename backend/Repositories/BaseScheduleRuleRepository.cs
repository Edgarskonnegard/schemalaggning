using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class BaseScheduleRuleRepository : IBaseScheduleRuleRepository
{
    private readonly AppDbContext _context;

    public BaseScheduleRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<BaseScheduleRule>> GetAllWithDetailsAsync()
    {
        return _context.BaseScheduleRules
            .AsNoTracking()
            .Include(rule => rule.Employee)
            .Include(rule => rule.ShiftType)
            .ToListAsync();
    }

    public Task<List<BaseScheduleRule>> GetByEmployeeIdAsync(int employeeId)
    {
        return _context.BaseScheduleRules
            .AsNoTracking()
            .Include(rule => rule.Employee)
            .Include(rule => rule.ShiftType)
            .Where(rule => rule.EmployeeId == employeeId)
            .OrderBy(rule => rule.WeekInCycle)
            .ThenBy(rule => rule.DayOfWeek)
            .ToListAsync();
    }

    public Task<BaseScheduleRule?> GetByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek)
    {
        return _context.BaseScheduleRules
            .Include(rule => rule.Employee)
            .Include(rule => rule.ShiftType)
            .FirstOrDefaultAsync(rule =>
                rule.EmployeeId == employeeId &&
                rule.WeekInCycle == weekInCycle &&
                rule.DayOfWeek == dayOfWeek);
    }

    public async Task<BaseScheduleRule> CreateAsync(BaseScheduleRule rule)
    {
        _context.BaseScheduleRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> UpdateAsync(BaseScheduleRule rule)
    {
        _context.BaseScheduleRules.Update(rule);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rule = await _context.BaseScheduleRules.FindAsync(id);
        if (rule is null)
        {
            return false;
        }

        _context.BaseScheduleRules.Remove(rule);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek)
    {
        var rule = await _context.BaseScheduleRules
            .FirstOrDefaultAsync(baseRule =>
                baseRule.EmployeeId == employeeId &&
                baseRule.WeekInCycle == weekInCycle &&
                baseRule.DayOfWeek == dayOfWeek);

        if (rule is null)
        {
            return false;
        }

        _context.BaseScheduleRules.Remove(rule);
        return await _context.SaveChangesAsync() > 0;
    }
}
