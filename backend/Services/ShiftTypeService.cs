using Schemalaggning.DTOs.ShiftTypes;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ShiftTypeService : IShiftTypeService
{
    private readonly IShiftTypeRepository _shiftTypeRepository;
    private readonly IRoleRepository _roleRepository;

    public ShiftTypeService(IShiftTypeRepository shiftTypeRepository, IRoleRepository roleRepository)
    {
        _shiftTypeRepository = shiftTypeRepository;
        _roleRepository = roleRepository;
    }

    public async Task<List<ShiftTypeReadDto>> GetAllAsync()
    {
        var shiftTypes = await _shiftTypeRepository.GetAllAsync();
        return shiftTypes.Select(shiftType => shiftType.ToReadDto()).ToList();
    }

    public async Task<ShiftTypeReadDto?> GetByIdAsync(int id)
    {
        var shiftType = await _shiftTypeRepository.GetByIdAsync(id);
        return shiftType?.ToReadDto();
    }

    public async Task<ShiftTypeReadDto> CreateAsync(ShiftTypeCreateDto dto)
    {
        await ValidateShiftTypeAsync(dto.Name, dto.RoleId, dto.DefaultStartTime, dto.DefaultEndTime);

        var shiftType = await _shiftTypeRepository.CreateAsync(new ShiftType
        {
            Name = dto.Name.Trim(),
            RoleId = dto.RoleId,
            DefaultStartTime = dto.DefaultStartTime,
            DefaultEndTime = dto.DefaultEndTime
        });

        var created = await _shiftTypeRepository.GetByIdAsync(shiftType.Id);
        return created!.ToReadDto();
    }

    public async Task<bool> UpdateAsync(int id, ShiftTypeUpdateDto dto)
    {
        await ValidateShiftTypeAsync(dto.Name, dto.RoleId, dto.DefaultStartTime, dto.DefaultEndTime);

        var shiftType = await _shiftTypeRepository.GetByIdAsync(id);
        if (shiftType is null)
        {
            return false;
        }

        shiftType.Name = dto.Name.Trim();
        shiftType.RoleId = dto.RoleId;
        shiftType.DefaultStartTime = dto.DefaultStartTime;
        shiftType.DefaultEndTime = dto.DefaultEndTime;

        return await _shiftTypeRepository.UpdateAsync(shiftType);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return _shiftTypeRepository.DeleteAsync(id);
    }

    private async Task ValidateShiftTypeAsync(string name, int roleId, TimeOnly startTime, TimeOnly endTime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shift type name is required.");
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("Default end time must be after default start time.");
        }

        if (!await _roleRepository.ExistsAsync(roleId))
        {
            throw new InvalidOperationException($"Role {roleId} does not exist.");
        }
    }
}
