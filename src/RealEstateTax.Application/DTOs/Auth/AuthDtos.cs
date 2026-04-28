namespace RealEstateTax.Application.DTOs.Auth;

public record LoginRequest(string Username, string Password);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Username,
    string Email,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
