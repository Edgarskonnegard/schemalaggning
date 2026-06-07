using Schemalaggning.DTOs.Employees;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository,
        IStoreRepository storeRepository,
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher)
    {
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _storeRepository = storeRepository;
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
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
        await ValidateEmployeeAsync(dto.Name, dto.StoreId, dto.RoleId, dto.EmploymentPercentage);
        await ValidateAccountFieldsAsync(
            dto.AccountEmail,
            dto.AccountPassword,
            dto.AccountAccessRole,
            requirePassword: HasAccountEmail(dto.AccountEmail),
            existingAccountId: null);

        var employee = await _employeeRepository.CreateAsync(new Employee
        {
            Name = dto.Name.Trim(),
            StoreId = dto.StoreId,
            RoleId = dto.RoleId,
            EmploymentPercentage = dto.EmploymentPercentage
        });

        if (HasAccountEmail(dto.AccountEmail))
        {
            await _userAccountRepository.CreateAsync(new UserAccount
            {
                Email = NormalizeEmail(dto.AccountEmail!),
                PasswordHash = _passwordHasher.Hash(dto.AccountPassword!),
                AccessRole = NormalizeAccessRole(dto.AccountAccessRole),
                EmployeeId = employee.Id,
                StoreId = dto.StoreId,
                IsActive = dto.AccountIsActive
            });
        }

        var created = await _employeeRepository.GetByIdAsync(employee.Id);
        return created!.ToReadDto();
    }

    public async Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto)
    {
        await ValidateEmployeeAsync(dto.Name, dto.StoreId, dto.RoleId, dto.EmploymentPercentage);

        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is null)
        {
            return false;
        }

        var existingAccount = await _userAccountRepository.GetByEmployeeIdAsync(id);
        var shouldHaveAccount = HasAccountEmail(dto.AccountEmail);
        await ValidateAccountFieldsAsync(
            dto.AccountEmail,
            dto.AccountPassword,
            dto.AccountAccessRole,
            requirePassword: shouldHaveAccount && existingAccount is null,
            existingAccountId: existingAccount?.Id);

        employee.Name = dto.Name.Trim();
        employee.StoreId = dto.StoreId;
        employee.RoleId = dto.RoleId;
        employee.EmploymentPercentage = dto.EmploymentPercentage;

        var employeeUpdated = await _employeeRepository.UpdateAsync(employee);

        if (shouldHaveAccount)
        {
            if (existingAccount is null)
            {
                await _userAccountRepository.CreateAsync(new UserAccount
                {
                    Email = NormalizeEmail(dto.AccountEmail!),
                    PasswordHash = _passwordHasher.Hash(dto.AccountPassword!),
                    AccessRole = NormalizeAccessRole(dto.AccountAccessRole),
                    EmployeeId = employee.Id,
                    StoreId = dto.StoreId,
                    IsActive = dto.AccountIsActive
                });
            }
            else
            {
                existingAccount.Email = NormalizeEmail(dto.AccountEmail!);
                existingAccount.AccessRole = NormalizeAccessRole(dto.AccountAccessRole);
                existingAccount.StoreId = dto.StoreId;
                existingAccount.IsActive = dto.AccountIsActive;

                if (!string.IsNullOrWhiteSpace(dto.AccountPassword))
                {
                    existingAccount.PasswordHash = _passwordHasher.Hash(dto.AccountPassword);
                }

                await _userAccountRepository.UpdateAsync(existingAccount);
            }
        }
        else if (existingAccount is not null)
        {
            existingAccount.IsActive = false;
            await _userAccountRepository.UpdateAsync(existingAccount);
        }

        return employeeUpdated;
    }

    public Task<bool> DeleteAsync(int id)
    {
        return _employeeRepository.DeleteAsync(id);
    }

    private async Task ValidateEmployeeAsync(string name, int storeId, int roleId, decimal employmentPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Employee name is required.");
        }

        if (employmentPercentage < 0 || employmentPercentage > 100)
        {
            throw new ArgumentException("Employment percentage must be between 0 and 100.");
        }

        if (!await _storeRepository.ExistsAsync(storeId))
        {
            throw new InvalidOperationException($"Store {storeId} does not exist.");
        }

        if (!await _roleRepository.ExistsAsync(roleId))
        {
            throw new InvalidOperationException($"Role {roleId} does not exist.");
        }
    }

    private async Task ValidateAccountFieldsAsync(
        string? email,
        string? password,
        string accessRole,
        bool requirePassword,
        int? existingAccountId)
    {
        if (!HasAccountEmail(email))
        {
            return;
        }

        if (!accessRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
            !accessRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Access role must be Admin or Employee.");
        }

        if (requirePassword && (string.IsNullOrWhiteSpace(password) || password.Length < 8))
        {
            throw new ArgumentException("Password must be at least 8 characters when creating an account.");
        }

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters.");
        }

        if (existingAccountId is null)
        {
            if (await _userAccountRepository.EmailExistsAsync(email!))
            {
                throw new InvalidOperationException("Email is already in use.");
            }
        }
        else if (await _userAccountRepository.EmailExistsForOtherAccountAsync(email!, existingAccountId.Value))
        {
            throw new InvalidOperationException("Email is already in use.");
        }
    }

    private static bool HasAccountEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeAccessRole(string accessRole)
    {
        return accessRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Employee";
    }
}
