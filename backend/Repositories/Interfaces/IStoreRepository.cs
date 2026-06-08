using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IStoreRepository
{
    Task<List<Store>> GetAllAsync();
    Task<Store?> GetByIdAsync(int id);
    Task<Store> CreateAsync(Store store);
    Task<bool> UpdateAsync(Store store);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
