using Mapster;
using RealEstateTax.Application.DTOs.Admin;
using RealEstateTax.Application.DTOs.Appeals;
using RealEstateTax.Application.DTOs.Bills;
using RealEstateTax.Application.DTOs.Exemptions;
using RealEstateTax.Application.DTOs.FieldSurveys;
using RealEstateTax.Application.DTOs.Integrations;
using RealEstateTax.Application.DTOs.Payments;
using RealEstateTax.Application.DTOs.Properties;
using RealEstateTax.Application.DTOs.Risk;
using RealEstateTax.Application.DTOs.TaxAssessments;
using RealEstateTax.Application.DTOs.Taxpayers;
using RealEstateTax.Application.DTOs.Valuations;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Application.Mappings;

public static class MappingConfig
{
    public static void Configure()
    {
        // Taxpayer
        TypeAdapterConfig<Taxpayer, TaxpayerDto>.NewConfig()
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.PropertyCount, src => src.PropertyOwnerships.Count(o => o.IsCurrent));

        TypeAdapterConfig<Taxpayer, TaxpayerDetailDto>.NewConfig()
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.PropertyCount, src => src.PropertyOwnerships.Count(o => o.IsCurrent))
            .Map(dest => dest.Properties, src => src.PropertyOwnerships.Adapt<List<PropertyOwnershipSummaryDto>>());

        TypeAdapterConfig<PropertyOwnership, PropertyOwnershipSummaryDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.Address, src => src.Property.FullAddress ?? src.Property.StreetAddress);

        // Property
        TypeAdapterConfig<Property, PropertyDto>.NewConfig()
            .Map(dest => dest.Latitude, src => src.Location != null ? src.Location.Latitude : null)
            .Map(dest => dest.Longitude, src => src.Location != null ? src.Location.Longitude : null)
            .Map(dest => dest.PrimaryOwnerName, src => src.Ownerships
                .Where(o => o.IsCurrent)
                .OrderByDescending(o => o.OwnershipPercentage)
                .Select(o => o.Taxpayer.FullName)
                .FirstOrDefault());

        TypeAdapterConfig<PropertyLocation, PropertyLocationDto>.NewConfig();

        TypeAdapterConfig<PropertyOwnership, PropertyOwnershipDto>.NewConfig()
            .Map(dest => dest.TaxpayerName, src => src.Taxpayer.FullName)
            .Map(dest => dest.TaxpayerNationalId, src => src.Taxpayer.NationalId);

        TypeAdapterConfig<PropertyUnit, PropertyUnitDto>.NewConfig();

        // Field Survey
        TypeAdapterConfig<FieldSurvey, FieldSurveyDto>.NewConfig()
            .Map(dest => dest.AssignedToName, src => src.AssignedToUser.FullName)
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.PropertyAddress, src => src.Property.FullAddress ?? src.Property.StreetAddress);

        TypeAdapterConfig<SurveyAttachment, SurveyAttachmentDto>.NewConfig()
            .Map(dest => dest.FileUrl, src => src.StoragePath);

        // Valuation
        TypeAdapterConfig<Valuation, ValuationDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode);

        // Tax Assessment
        TypeAdapterConfig<TaxAssessment, TaxAssessmentDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode);

        // Tax Bill
        TypeAdapterConfig<TaxBill, TaxBillDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.PropertyAddress, src => src.Property.FullAddress ?? src.Property.StreetAddress)
            .Map(dest => dest.TaxpayerName, src => src.Taxpayer.FullName)
            .Map(dest => dest.RemainingAmount, src => src.RemainingAmount);

        // Payment
        TypeAdapterConfig<Payment, PaymentDto>.NewConfig()
            .Map(dest => dest.BillNumber, src => src.TaxBill.BillNumber)
            .Map(dest => dest.TaxpayerName, src => src.Taxpayer.FullName);

        // Appeal
        TypeAdapterConfig<Appeal, AppealDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.TaxpayerName, src => src.Taxpayer.FullName)
            .Map(dest => dest.AssignedToName, src => src.AssignedToUser != null ? src.AssignedToUser.FullName : null);

        TypeAdapterConfig<AppealDocument, AppealDocumentDto>.NewConfig()
            .Map(dest => dest.FileUrl, src => src.StoragePath);

        // Exemption
        TypeAdapterConfig<Exemption, ExemptionDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.TaxpayerName, src => src.Taxpayer.FullName);

        // Risk
        TypeAdapterConfig<RiskScore, RiskScoreDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.Level, src => src.Level.ToString())
            .Map(dest => dest.RiskFactors, src => src.RiskFactors != null

                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(src.RiskFactors, (System.Text.Json.JsonSerializerOptions?)null)
                : new List<string>())
            .Map(dest => dest.Recommendations, src => src.Recommendations != null
 
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(src.Recommendations, (System.Text.Json.JsonSerializerOptions?)null)
                : new List<string>());

        TypeAdapterConfig<FraudFlag, FraudFlagDto>.NewConfig()
            .Map(dest => dest.PropertyCode, src => src.Property.PropertyCode)
            .Map(dest => dest.RaisedByName, src => src.RaisedByUser != null ? src.RaisedByUser.FullName : null);

        // Integration
        TypeAdapterConfig<IntegrationRequest, IntegrationRequestDto>.NewConfig()
            .Map(dest => dest.ExternalEntityCode, src => src.ExternalEntity.Code)
            .Map(dest => dest.ExternalEntityName, src => src.ExternalEntity.Name);

        // Audit
        TypeAdapterConfig<AuditLog, AuditLogDto>.NewConfig();
    }
}
