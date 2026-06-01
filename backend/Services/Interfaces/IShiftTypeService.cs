using Schemalaggning.DTOs.ShiftTypes;

namespace Schemalaggning.Services.Interfaces;

public interface IShiftTypeService
{
    Task<List<ShiftTypeReadDto>> GetAllAsync();
    Task<ShiftTypeReadDto?> GetByIdAsync(int id);
    Task<ShiftTypeReadDto> CreateAsync(ShiftTypeCreateDto dto);
    Task<bool> UpdateAsync(int id, ShiftTypeUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
