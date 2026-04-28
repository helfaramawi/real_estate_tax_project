using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    (bool IsValid, string? Username) ValidateRefreshToken(string token);
}
