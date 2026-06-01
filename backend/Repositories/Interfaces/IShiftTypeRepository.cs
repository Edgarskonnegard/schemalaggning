using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IShiftTypeRepository
{
    Task<List<ShiftType>> GetAllAsync();
    Task<ShiftType?> GetByIdAsync(int id);
    Task<List<ShiftType>> GetByRoleAsync(int roleId);
    Task<ShiftType> CreateAsync(ShiftType shiftType);
    Task<bool> UpdateAsync(ShiftType shiftType);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
