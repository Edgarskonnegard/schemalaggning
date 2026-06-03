using Schemalaggning.Models;

namespace Schemalaggning.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccount userAccount);
}
