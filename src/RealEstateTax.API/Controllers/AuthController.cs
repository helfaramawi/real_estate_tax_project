using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.DTOs.Auth;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Authentication — login, token refresh, and password management.</summary>
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

    /// <summary>Authenticate with username and password; returns JWT access and refresh tokens.</summary>
    /// <remarks>
    /// Accounts are locked for 30 minutes after 5 consecutive failed attempts.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(423)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>Exchange a valid refresh token for a new access token.</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>Change the currently authenticated user's password.</summary>
    /// <remarks>New password must be at least 8 characters with uppercase, lowercase, digit, and special character.</remarks>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized();
        var result = await _authService.ChangePasswordAsync(_currentUser.UserId.Value, request, ct);
        return result.ToActionResult(this);
    }
}
