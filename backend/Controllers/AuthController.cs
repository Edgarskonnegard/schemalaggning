using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.Auth;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<List<UserAccountReadDto>>> GetAccounts()
    {
        return Ok(await _authService.GetAccountsAsync());
    }

    [HttpPost("accounts")]
    public async Task<ActionResult<UserAccountReadDto>> CreateAccount(UserAccountCreateDto dto)
    {
        try
        {
            return Ok(await _authService.CreateAccountAsync(dto));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
    {
        try
        {
            return Ok(await _authService.LoginAsync(dto));
        }
        catch (InvalidOperationException exception)
        {
            return Unauthorized(exception.Message);
        }
    }
}
