using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

public class Taxpayer : BaseEntity
{
    public string TaxpayerCode { get; set; } = string.Empty;   // System-generated unique code
    public string NationalId { get; set; } = string.Empty;     // Egyptian National ID (14 digits)
    public string? TaxRegistrationNumber { get; set; }
    public string? CommercialRegistrationNumber { get; set; }

    // Personal info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; } = "EGY";

    // Corporate
    public bool IsCorporate { get; set; }
    public string? CompanyName { get; set; }
    public string? LegalForm { get; set; }

    // Contact
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternativePhone { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // Flags
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public bool IsDeceased { get; set; }
    public DateTime? DeceasedAt { get; set; }

    public Guid? LinkedUserId { get; set; }
    public User? LinkedUser { get; set; }

    public ICollection<PropertyOwnership> PropertyOwnerships { get; set; } = [];
    public ICollection<Appeal> Appeals { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];

    public string FullName => IsCorporate ? CompanyName ?? string.Empty : $"{FirstName} {LastName}";
}
