using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class PropertyOwnership : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid? PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }

    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public OwnershipType OwnershipType { get; set; }
    public decimal OwnershipPercentage { get; set; } = 100m;  // 0–100
    public DateTime OwnershipStartDate { get; set; }
    public DateTime? OwnershipEndDate { get; set; }
    public bool IsCurrent { get; set; } = true;

    // Document references
    public string? TitleDeedNumber { get; set; }
    public DateTime? TitleDeedDate { get; set; }
    public string? RegistrationAuthority { get; set; }
    public string? NotarizationReference { get; set; }
    public string? InheritanceReference { get; set; }

    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
