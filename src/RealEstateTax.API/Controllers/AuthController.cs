using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.DTOs.Auth;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Login with username and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>Refresh an expired access token.</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>Change current user's password.</summary>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized();
        var result = await _authService.ChangePasswordAsync(_currentUser.UserId.Value, request, ct);
        return result.ToActionResult(this);
    }
}
