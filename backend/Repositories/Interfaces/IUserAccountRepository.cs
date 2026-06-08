using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IUserAccountRepository
{
    Task<List<UserAccount>> GetAllAsync();
    Task<UserAccount?> GetByIdAsync(int id);
    Task<UserAccount?> GetByEmailAsync(string email);
    Task<UserAccount?> GetByEmployeeIdAsync(int employeeId);
    Task<UserAccount> CreateAsync(UserAccount userAccount);
    Task<bool> UpdateAsync(UserAccount userAccount);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EmailExistsForOtherAccountAsync(string email, int accountId);
}
