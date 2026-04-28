using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Auth;

namespace RealEstateTax.Application.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
