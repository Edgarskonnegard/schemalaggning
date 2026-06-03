using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccount userAccount)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "Schemalaggning";
        var audience = _configuration["Jwt:Audience"] ?? "Schemalaggning";
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is missing.");
        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, userAccount.Email),
            new(ClaimTypes.Role, userAccount.AccessRole),
            new("userId", userAccount.Id.ToString())
        };

        if (userAccount.EmployeeId is not null)
        {
            claims.Add(new Claim("employeeId", userAccount.EmployeeId.Value.ToString()));
        }

        if (userAccount.StoreId is not null)
        {
            claims.Add(new Claim("storeId", userAccount.StoreId.Value.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
