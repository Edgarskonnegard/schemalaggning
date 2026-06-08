using Schemalaggning.DTOs.Auth;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class AuthService : IAuthService
{
    private static readonly HashSet<string> ValidAccessRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Employee"
    };

    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserAccountRepository userAccountRepository,
        IEmployeeRepository employeeRepository,
        IStoreRepository storeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userAccountRepository = userAccountRepository;
        _employeeRepository = employeeRepository;
        _storeRepository = storeRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<List<UserAccountReadDto>> GetAccountsAsync()
    {
        var accounts = await _userAccountRepository.GetAllAsync();
        return accounts.Select(account => account.ToReadDto()).ToList();
    }

    public async Task<UserAccountReadDto> CreateAccountAsync(UserAccountCreateDto dto)
    {
        await ValidateCreateAccountAsync(dto);

        var account = await _userAccountRepository.CreateAsync(new UserAccount
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(dto.Password),
            AccessRole = NormalizeAccessRole(dto.AccessRole),
            EmployeeId = dto.EmployeeId,
            StoreId = dto.StoreId,
            IsActive = true
        });

        return account.ToReadDto();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var account = await _userAccountRepository.GetByEmailAsync(dto.Email);
        if (account is null || !account.IsActive || !_passwordHasher.Verify(dto.Password, account.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var token = _jwtTokenService.CreateToken(account);
        return new AuthResponseDto
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = account.ToReadDto()
        };
    }

    private async Task ValidateCreateAccountAsync(UserAccountCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters.");
        }

        if (!ValidAccessRoles.Contains(dto.AccessRole))
        {
            throw new ArgumentException("Access role must be Admin or Employee.");
        }

        if (await _userAccountRepository.EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        var accessRole = NormalizeAccessRole(dto.AccessRole);
        if (accessRole == "Employee" && dto.EmployeeId is null)
        {
            throw new ArgumentException("Employee accounts must be connected to an employee.");
        }

        if (dto.EmployeeId is not null && await _employeeRepository.GetByIdAsync(dto.EmployeeId.Value) is null)
        {
            throw new InvalidOperationException($"Employee {dto.EmployeeId} does not exist.");
        }

        if (dto.StoreId is not null && !await _storeRepository.ExistsAsync(dto.StoreId.Value))
        {
            throw new InvalidOperationException($"Store {dto.StoreId} does not exist.");
        }
    }

    private static string NormalizeAccessRole(string accessRole)
    {
        return accessRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Employee";
    }
}
