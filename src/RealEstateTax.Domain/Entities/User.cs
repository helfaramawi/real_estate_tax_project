using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
}
