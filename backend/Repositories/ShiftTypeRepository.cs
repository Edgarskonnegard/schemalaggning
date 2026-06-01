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
            .Include(shiftType => shiftType.Role)
            .OrderBy(shiftType => shiftType.Name)
            .ToListAsync();
    }

    public Task<ShiftType?> GetByIdAsync(int id)
    {
        return _context.ShiftTypes
            .Include(shiftType => shiftType.Role)
            .FirstOrDefaultAsync(shiftType => shiftType.Id == id);
    }

    public Task<List<ShiftType>> GetByRoleAsync(int roleId)
    {
        return _context.ShiftTypes
            .AsNoTracking()
            .Include(shiftType => shiftType.Role)
            .Where(shiftType => shiftType.RoleId == roleId)
            .OrderBy(shiftType => shiftType.Name)
            .ToListAsync();
    }

    public async Task<ShiftType> CreateAsync(ShiftType shiftType)
    {
        _context.ShiftTypes.Add(shiftType);
        await _context.SaveChangesAsync();
        return shiftType;
    }

    public async Task<bool> UpdateAsync(ShiftType shiftType)
    {
        _context.ShiftTypes.Update(shiftType);
        return await _context.SaveChangesAsync() > 0;
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
