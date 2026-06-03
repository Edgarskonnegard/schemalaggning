using Schemalaggning.DTOs.Auth;

namespace Schemalaggning.Services.Interfaces;

public interface IAuthService
{
    Task<List<UserAccountReadDto>> GetAccountsAsync();
    Task<UserAccountReadDto> CreateAccountAsync(UserAccountCreateDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
