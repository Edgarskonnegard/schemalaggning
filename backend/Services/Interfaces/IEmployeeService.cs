using Schemalaggning.DTOs.Employees;

namespace Schemalaggning.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeReadDto>> GetAllAsync();
    Task<EmployeeReadDto?> GetByIdAsync(int id);
    Task<EmployeeReadDto?> GetDetailsAsync(int id);
    Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto);
    Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateAllowedShiftTypesAsync(int employeeId, List<int> shiftTypeIds);
}
