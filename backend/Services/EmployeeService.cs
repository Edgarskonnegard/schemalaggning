using Schemalaggning.DTOs.Employees;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository)
    {
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
    }

    public async Task<List<EmployeeReadDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(employee => employee.ToReadDto()).ToList();
    }

    public async Task<EmployeeReadDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        return employee?.ToReadDto();
    }

    public async Task<EmployeeReadDto?> GetDetailsAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdWithDetailsAsync(id);
        return employee?.ToReadDto();
    }

    public async Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto)
    {
        await ValidateEmployeeAsync(dto.Name, dto.RoleId, dto.EmploymentPercentage);

        var employee = await _employeeRepository.CreateAsync(new Employee
        {
            Name = dto.Name.Trim(),
            RoleId = dto.RoleId,
            EmploymentPercentage = dto.EmploymentPercentage
        });

        var created = await _employeeRepository.GetByIdAsync(employee.Id);
        return created!.ToReadDto();
    }

    public async Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto)
    {
        await ValidateEmployeeAsync(dto.Name, dto.RoleId, dto.EmploymentPercentage);

        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is null)
        {
            return false;
        }

        employee.Name = dto.Name.Trim();
        employee.RoleId = dto.RoleId;
        employee.EmploymentPercentage = dto.EmploymentPercentage;

        return await _employeeRepository.UpdateAsync(employee);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return _employeeRepository.DeleteAsync(id);
    }

    private async Task ValidateEmployeeAsync(string name, int roleId, decimal employmentPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Employee name is required.");
        }

        if (employmentPercentage < 0 || employmentPercentage > 100)
        {
            throw new ArgumentException("Employment percentage must be between 0 and 100.");
        }

        if (!await _roleRepository.ExistsAsync(roleId))
        {
            throw new InvalidOperationException($"Role {roleId} does not exist.");
        }
    }
}
