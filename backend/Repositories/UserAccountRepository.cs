using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly AppDbContext _context;

    public UserAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<UserAccount>> GetAllAsync()
    {
        return _context.UserAccounts
            .AsNoTracking()
            .OrderBy(userAccount => userAccount.Email)
            .ToListAsync();
    }

    public Task<UserAccount?> GetByIdAsync(int id)
    {
        return _context.UserAccounts.FindAsync(id).AsTask();
    }

    public Task<UserAccount?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.UserAccounts
            .FirstOrDefaultAsync(userAccount => userAccount.Email == normalizedEmail);
    }

    public async Task<UserAccount> CreateAsync(UserAccount userAccount)
    {
        _context.UserAccounts.Add(userAccount);
        await _context.SaveChangesAsync();
        return userAccount;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.UserAccounts.AnyAsync(userAccount => userAccount.Email == normalizedEmail);
    }
}
