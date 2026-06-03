using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class ShiftTypeRepository : IShiftTypeRepository
{
    private readonly AppDbContext _context;

    public ShiftTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<ShiftType>> GetAllAsync()
    {
        return _context.ShiftTypes
            .AsNoTracking()
            .Include(shiftType => shiftType.RoleShiftTypes)
                .ThenInclude(roleShiftType => roleShiftType.Role)
            .OrderBy(shiftType => shiftType.Name)
            .ToListAsync();
    }

    public Task<ShiftType?> GetByIdAsync(int id)
    {
        return _context.ShiftTypes
            .Include(shiftType => shiftType.RoleShiftTypes)
                .ThenInclude(roleShiftType => roleShiftType.Role)
            .FirstOrDefaultAsync(shiftType => shiftType.Id == id);
    }

    public Task<List<ShiftType>> GetByRoleAsync(int roleId)
    {
        return _context.ShiftTypes
            .AsNoTracking()
            .Include(shiftType => shiftType.RoleShiftTypes)
                .ThenInclude(roleShiftType => roleShiftType.Role)
            .Where(shiftType => shiftType.RoleShiftTypes.Any(roleShiftType => roleShiftType.RoleId == roleId))
            .OrderBy(shiftType => shiftType.Name)
            .ToListAsync();
    }

    public async Task<bool> HasAnyInvalidRoleIdsAsync(List<int> roleIds)
    {
        var uniqueRoleIds = roleIds.Distinct().ToList();
        var existingCount = await _context.Roles
            .CountAsync(role => uniqueRoleIds.Contains(role.Id));

        return existingCount != uniqueRoleIds.Count;
    }

    public async Task<ShiftType> CreateAsync(ShiftType shiftType)
    {
        _context.ShiftTypes.Add(shiftType);
        await _context.SaveChangesAsync();
        return shiftType;
    }

    public async Task<bool> UpdateAsync(ShiftType shiftType)
    {
        var roleIds = shiftType.RoleShiftTypes
            .Select(roleShiftType => roleShiftType.RoleId)
            .Distinct()
            .ToList();

        var existingRoleShiftTypes = await _context.RoleShiftTypes
            .Where(roleShiftType => roleShiftType.ShiftTypeId == shiftType.Id)
            .ToListAsync();

        _context.RoleShiftTypes.RemoveRange(existingRoleShiftTypes);
        shiftType.RoleShiftTypes.Clear();
        _context.ShiftTypes.Update(shiftType);

        var changedRows = await _context.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            _context.RoleShiftTypes.Add(new RoleShiftType
            {
                RoleId = roleId,
                ShiftTypeId = shiftType.Id
            });
        }

        changedRows += await _context.SaveChangesAsync();
        return changedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var shiftType = await _context.ShiftTypes.FindAsync(id);
        if (shiftType is null)
        {
            return false;
        }

        _context.ShiftTypes.Remove(shiftType);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _context.ShiftTypes.AnyAsync(shiftType => shiftType.Id == id);
    }
}
