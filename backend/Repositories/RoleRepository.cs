using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Role>> GetAllAsync()
    {
        return _context.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync();
    }

    public Task<Role?> GetByIdAsync(int id)
    {
        return _context.Roles.FindAsync(id).AsTask();
    }

    public async Task<Role> CreateAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<bool> UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null)
        {
            return false;
        }

        _context.Roles.Remove(role);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _context.Roles.AnyAsync(role => role.Id == id);
    }
}
