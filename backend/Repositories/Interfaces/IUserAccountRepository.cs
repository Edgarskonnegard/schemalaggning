using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IUserAccountRepository
{
    Task<List<UserAccount>> GetAllAsync();
    Task<UserAccount?> GetByIdAsync(int id);
    Task<UserAccount?> GetByEmailAsync(string email);
    Task<UserAccount> CreateAsync(UserAccount userAccount);
    Task<bool> EmailExistsAsync(string email);
}
