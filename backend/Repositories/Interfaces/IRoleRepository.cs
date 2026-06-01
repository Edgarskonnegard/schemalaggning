using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role> CreateAsync(Role role);
    Task<bool> UpdateAsync(Role role);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
