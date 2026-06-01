using Schemalaggning.DTOs.Roles;

namespace Schemalaggning.Services.Interfaces;

public interface IRoleService
{
    Task<List<RoleReadDto>> GetAllAsync();
    Task<RoleReadDto?> GetByIdAsync(int id);
    Task<RoleReadDto> CreateAsync(RoleCreateDto dto);
    Task<bool> UpdateAsync(int id, RoleCreateDto dto);
    Task<bool> DeleteAsync(int id);
}
