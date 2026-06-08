using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class StoreCoverageRuleRepository : IStoreCoverageRuleRepository
{
    private readonly AppDbContext _context;

    public StoreCoverageRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<StoreCoverageRule>> GetByStoreIdAsync(int storeId)
    {
        return _context.StoreCoverageRules
            .AsNoTracking()
            .Include(rule => rule.Store)
            .Include(rule => rule.ShiftType)
            .Where(rule => rule.StoreId == storeId)
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .ToListAsync();
    }

    public Task<StoreCoverageRule?> GetByStoreDayAndShiftTypeAsync(
        int storeId,
        DayOfWeek dayOfWeek,
        int shiftTypeId)
    {
        return _context.StoreCoverageRules
            .Include(rule => rule.Store)
            .Include(rule => rule.ShiftType)
            .FirstOrDefaultAsync(rule =>
                rule.StoreId == storeId &&
                rule.DayOfWeek == dayOfWeek &&
                rule.ShiftTypeId == shiftTypeId);
    }

    public async Task<StoreCoverageRule> CreateAsync(StoreCoverageRule rule)
    {
        _context.StoreCoverageRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> UpdateAsync(StoreCoverageRule rule)
    {
        _context.StoreCoverageRules.Update(rule);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int storeId, int id)
    {
        var rule = await _context.StoreCoverageRules
            .FirstOrDefaultAsync(rule => rule.Id == id && rule.StoreId == storeId);

        if (rule is null)
        {
            return false;
        }

        _context.StoreCoverageRules.Remove(rule);
        return await _context.SaveChangesAsync() > 0;
    }
}
