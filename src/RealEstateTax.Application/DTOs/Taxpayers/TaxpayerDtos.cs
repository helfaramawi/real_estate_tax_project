namespace RealEstateTax.Application.DTOs.Taxpayers;

public class TaxpayerDto
{
    public Guid Id { get; set; }
    public string TaxpayerCode { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string? TaxRegistrationNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsCorporate { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public bool IsVerified { get; set; }
    public bool IsDeceased { get; set; }
    public int PropertyCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaxpayerDetailDto : TaxpayerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? LegalForm { get; set; }
    public string? AlternativePhone { get; set; }
    public string? PostalCode { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public List<PropertyOwnershipSummaryDto> Properties { get; set; } = [];
}

public class CreateTaxpayerRequest
{
    public string NationalId { get; set; } = string.Empty;
    public string? TaxRegistrationNumber { get; set; }
    public bool IsCorporate { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string? CompanyName { get; set; }
    public string? LegalForm { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

public class UpdateTaxpayerRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternativePhone { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

public class PropertyOwnershipSummaryDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public bool IsCurrent { get; set; }
}
