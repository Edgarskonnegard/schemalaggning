using Schemalaggning.DTOs.Roles;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleReadDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(role => role.ToReadDto()).ToList();
    }

    public async Task<RoleReadDto?> GetByIdAsync(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role?.ToReadDto();
    }

    public async Task<RoleReadDto> CreateAsync(RoleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Role name is required.");
        }

        var role = await _roleRepository.CreateAsync(new Role
        {
            Name = dto.Name.Trim()
        });

        return role.ToReadDto();
    }

    public async Task<bool> UpdateAsync(int id, RoleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Role name is required.");
        }

        var role = await _roleRepository.GetByIdAsync(id);
        if (role is null)
        {
            return false;
        }

        role.Name = dto.Name.Trim();
        return await _roleRepository.UpdateAsync(role);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return _roleRepository.DeleteAsync(id);
    }
}
