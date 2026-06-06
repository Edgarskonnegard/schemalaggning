using Schemalaggning.DTOs.StoreCoverageRules;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class StoreCoverageRuleService : IStoreCoverageRuleService
{
    private readonly IStoreCoverageRuleRepository _coverageRuleRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IShiftTypeRepository _shiftTypeRepository;

    public StoreCoverageRuleService(
        IStoreCoverageRuleRepository coverageRuleRepository,
        IStoreRepository storeRepository,
        IShiftTypeRepository shiftTypeRepository)
    {
        _coverageRuleRepository = coverageRuleRepository;
        _storeRepository = storeRepository;
        _shiftTypeRepository = shiftTypeRepository;
    }

    public async Task<List<StoreCoverageRuleReadDto>> GetByStoreIdAsync(int storeId)
    {
        var rules = await _coverageRuleRepository.GetByStoreIdAsync(storeId);
        return rules.Select(rule => rule.ToReadDto()).ToList();
    }

    public async Task<StoreCoverageRuleReadDto> SetRuleAsync(int storeId, StoreCoverageRuleCreateDto dto)
    {
        await ValidateRuleAsync(storeId, dto);

        var existingRule = await _coverageRuleRepository.GetByStoreDayAndShiftTypeAsync(
            storeId,
            dto.DayOfWeek,
            dto.ShiftTypeId);

        if (existingRule is null)
        {
            var created = await _coverageRuleRepository.CreateAsync(new StoreCoverageRule
            {
                StoreId = storeId,
                ShiftTypeId = dto.ShiftTypeId,
                DayOfWeek = dto.DayOfWeek,
                RequiredCount = dto.RequiredCount,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            });

            var rules = await _coverageRuleRepository.GetByStoreIdAsync(storeId);
            return rules.First(rule => rule.Id == created.Id).ToReadDto();
        }

        existingRule.RequiredCount = dto.RequiredCount;
        existingRule.StartTime = dto.StartTime;
        existingRule.EndTime = dto.EndTime;
        await _coverageRuleRepository.UpdateAsync(existingRule);

        var updatedRules = await _coverageRuleRepository.GetByStoreIdAsync(storeId);
        return updatedRules.First(rule => rule.Id == existingRule.Id).ToReadDto();
    }

    public Task<bool> DeleteAsync(int storeId, int id)
    {
        return _coverageRuleRepository.DeleteAsync(storeId, id);
    }

    private async Task ValidateRuleAsync(int storeId, StoreCoverageRuleCreateDto dto)
    {
        if (!await _storeRepository.ExistsAsync(storeId))
        {
            throw new InvalidOperationException($"Store {storeId} does not exist.");
        }

        if (!await _shiftTypeRepository.ExistsAsync(dto.ShiftTypeId))
        {
            throw new InvalidOperationException($"ShiftType {dto.ShiftTypeId} does not exist.");
        }

        if (dto.RequiredCount < 1)
        {
            throw new ArgumentException("Required count must be at least 1.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }
    }
}
