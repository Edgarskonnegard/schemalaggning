using Schemalaggning.DTOs.StoreCoverageRules;

namespace Schemalaggning.Services.Interfaces;

public interface IStoreCoverageRuleService
{
    Task<List<StoreCoverageRuleReadDto>> GetByStoreIdAsync(int storeId);
    Task<StoreCoverageRuleReadDto> SetRuleAsync(int storeId, StoreCoverageRuleCreateDto dto);
    Task<bool> DeleteAsync(int storeId, int id);
}
