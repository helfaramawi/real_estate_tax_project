using Mapster;
using Microsoft.EntityFrameworkCore;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Auth;
using RealEstateTax.Application.Services;

namespace RealEstateTax.Infrastructure.Services.ApplicationServices;

public class AuthAppService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;
    private const int RefreshTokenExpiryDays = 30;

    public AuthAppService(IApplicationDbContext db, IJwtTokenService jwt, IAuditService audit)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user != null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                await _db.SaveChangesAsync(ct);
            }
            await _audit.LogAsync("LOGIN_FAILED", "User", null, request.Username, null, null, false, "Invalid credentials", ct);
            return Result<TokenResponse>.Failure("Invalid username or password.");
        }

        if (!user.IsActive) return Result<TokenResponse>.Failure("Account is inactive.");
        if (user.IsLocked()) return Result<TokenResponse>.Failure("Account is temporarily locked.");

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var accessToken = _jwt.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("LOGIN_SUCCESS", "User", user.Id, user.Username, null, null, true, null, ct);

        return Result<TokenResponse>.Success(new TokenResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            user.Username,
            user.Email,
            roles,
            permissions
        ));
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == request.RefreshToken
                && u.RefreshTokenExpiry > DateTime.UtcNow
                && !u.IsDeleted, ct);

        if (user == null) return Result<TokenResponse>.Failure("Invalid or expired refresh token.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles.SelectMany(ur => ur.Role.RolePermissions).Select(rp => rp.Permission.Name).Distinct().ToList();

        var newAccess = _jwt.GenerateAccessToken(user, roles, permissions);
        var newRefresh = _jwt.GenerateRefreshToken();

        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        await _db.SaveChangesAsync(ct);

        return Result<TokenResponse>.Success(new TokenResponse(
            newAccess, newRefresh, DateTime.UtcNow.AddMinutes(60),
            user.Username, user.Email, roles, permissions));
    }

    public async Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user == null) return Result<bool>.NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<bool>.Failure("Current password is incorrect.");

        if (request.NewPassword != request.ConfirmNewPassword)
            return Result<bool>.Failure("New passwords do not match.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RefreshToken = null;  // Invalidate all sessions
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("PASSWORD_CHANGED", "User", userId, user.Username, null, null, true, null, ct);

        return Result<bool>.Success(true);
    }
}
