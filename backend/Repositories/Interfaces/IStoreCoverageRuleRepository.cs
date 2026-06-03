using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IStoreCoverageRuleRepository
{
    Task<List<StoreCoverageRule>> GetByStoreIdAsync(int storeId);
    Task<StoreCoverageRule?> GetByStoreWeekDayAndShiftTypeAsync(
        int storeId,
        int weekInCycle,
        DayOfWeek dayOfWeek,
        int shiftTypeId);
    Task<StoreCoverageRule> CreateAsync(StoreCoverageRule rule);
    Task<bool> UpdateAsync(StoreCoverageRule rule);
    Task<bool> DeleteAsync(int storeId, int id);
}
