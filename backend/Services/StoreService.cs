using Schemalaggning.DTOs.Stores;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;

    public StoreService(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    public async Task<List<StoreReadDto>> GetAllAsync()
    {
        var stores = await _storeRepository.GetAllAsync();
        return stores.Select(store => store.ToReadDto()).ToList();
    }

    public async Task<StoreReadDto?> GetByIdAsync(int id)
    {
        var store = await _storeRepository.GetByIdAsync(id);
        return store?.ToReadDto();
    }

    public async Task<StoreReadDto> CreateAsync(StoreCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Store name is required.");
        }

        var store = await _storeRepository.CreateAsync(new Store
        {
            Name = dto.Name.Trim()
        });

        return store.ToReadDto();
    }

    public async Task<bool> UpdateAsync(int id, StoreCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Store name is required.");
        }

        var store = await _storeRepository.GetByIdAsync(id);
        if (store is null)
        {
            return false;
        }

        store.Name = dto.Name.Trim();
        return await _storeRepository.UpdateAsync(store);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return _storeRepository.DeleteAsync(id);
    }
}
