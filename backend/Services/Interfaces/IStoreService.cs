using Schemalaggning.DTOs.Stores;

namespace Schemalaggning.Services.Interfaces;

public interface IStoreService
{
    Task<List<StoreReadDto>> GetAllAsync();
    Task<StoreReadDto?> GetByIdAsync(int id);
    Task<StoreReadDto> CreateAsync(StoreCreateDto dto);
    Task<bool> UpdateAsync(int id, StoreCreateDto dto);
    Task<bool> DeleteAsync(int id);
}
