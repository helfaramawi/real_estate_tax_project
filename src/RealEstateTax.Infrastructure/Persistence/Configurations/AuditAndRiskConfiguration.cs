using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Persistence.Configurations;

public class AppealConfiguration : IEntityTypeConfiguration<Appeal>
{
    public void Configure(EntityTypeBuilder<Appeal> builder)
    {
        builder.ToTable("appeals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppealCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.GroundsSummary).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DetailedStatement).HasMaxLength(10000);
        builder.Property(x => x.LegalBasis).HasMaxLength(2000);
        builder.Property(x => x.RequestedAssessmentValue).HasPrecision(18, 2);
        builder.Property(x => x.RevisedAssessmentValue).HasPrecision(18, 2);
        builder.Property(x => x.DecisionBy).HasMaxLength(100);
        builder.Property(x => x.DecisionNotes).HasMaxLength(2000);
        builder.Property(x => x.HearingNotes).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.Appeals).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.Taxpayer).WithMany(t => t.Appeals).HasForeignKey(x => x.TaxpayerId);
        builder.HasOne(x => x.TaxAssessment).WithMany().HasForeignKey(x => x.TaxAssessmentId).IsRequired(false);
        builder.HasOne(x => x.TaxBill).WithMany().HasForeignKey(x => x.TaxBillId).IsRequired(false);
        builder.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).IsRequired(false);

        builder.HasIndex(x => x.AppealCode).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();
    }
}

public class AppealDocumentConfiguration : IEntityTypeConfiguration<AppealDocument>
{
    public void Configure(EntityTypeBuilder<AppealDocument> builder)
    {
        builder.ToTable("appeal_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Appeal).WithMany(a => a.Documents).HasForeignKey(x => x.AppealId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ExemptionConfiguration : IEntityTypeConfiguration<Exemption>
{
    public void Configure(EntityTypeBuilder<Exemption> builder)
    {
        builder.ToTable("exemptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExemptionCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.JustificationSummary).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SupportingDocuments).HasMaxLength(5000);
        builder.Property(x => x.ExemptionPercentage).HasPrecision(5, 2);
        builder.Property(x => x.ExemptAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReviewedBy).HasMaxLength(100);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);
        builder.Property(x => x.ApprovalNotes).HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.RevocationReason).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.Exemptions).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.Taxpayer).WithMany().HasForeignKey(x => x.TaxpayerId);
        builder.HasOne(x => x.ExemptionRule).WithMany().HasForeignKey(x => x.ExemptionRuleId).IsRequired(false);

        builder.HasIndex(x => x.ExemptionCode).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.TemplateId).HasMaxLength(100);
        builder.Property(x => x.TemplateData).HasMaxLength(5000);
        builder.Property(x => x.RecipientAddress).HasMaxLength(200);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Taxpayer).WithMany(t => t.Notifications).HasForeignKey(x => x.TaxpayerId);
        builder.HasIndex(x => new { x.TaxpayerId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ExternalEntityConfiguration : IEntityTypeConfiguration<ExternalEntity>
{
    public void Configure(EntityTypeBuilder<ExternalEntity> builder)
    {
        builder.ToTable("external_entities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.BaseUrl).HasMaxLength(500);
        builder.Property(x => x.AuthType).HasMaxLength(50);
        builder.Property(x => x.ApiKeyHeader).HasMaxLength(100);
        builder.Property(x => x.ApiKeyValue).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class IntegrationRequestConfiguration : IEntityTypeConfiguration<IntegrationRequest>
{
    public void Configure(EntityTypeBuilder<IntegrationRequest> builder)
    {
        builder.ToTable("integration_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.ExternalEntity).WithMany(e => e.IntegrationRequests).HasForeignKey(x => x.ExternalEntityId);
        builder.HasIndex(x => new { x.ExternalEntityId, x.Status });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RiskScoreConfiguration : IEntityTypeConfiguration<RiskScore>
{
    public void Configure(EntityTypeBuilder<RiskScore> builder)
    {
        builder.ToTable("risk_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RiskFactors).HasMaxLength(5000);
        builder.Property(x => x.Recommendations).HasMaxLength(5000);
        builder.Property(x => x.CalculatedBy).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.RiskScores).HasForeignKey(x => x.PropertyId);
        builder.HasIndex(x => new { x.PropertyId, x.CalculatedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class FraudFlagConfiguration : IEntityTypeConfiguration<FraudFlag>
{
    public void Configure(EntityTypeBuilder<FraudFlag> builder)
    {
        builder.ToTable("fraud_flags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Evidence).HasMaxLength(5000);
        builder.Property(x => x.RaisedBySystem).HasMaxLength(100);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.FraudFlags).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.RaisedByUser).WithMany().HasForeignKey(x => x.RaisedByUserId).IsRequired(false);
        builder.HasOne(x => x.InvestigatedByUser).WithMany().HasForeignKey(x => x.InvestigatedByUserId).IsRequired(false);

        builder.HasIndex(x => new { x.PropertyId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(100);
        builder.Property(x => x.UserRole).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityCode).HasMaxLength(50);
        builder.Property(x => x.ChangeSummary).HasMaxLength(1000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.HttpMethod).HasMaxLength(10);
        builder.Property(x => x.RequestPath).HasMaxLength(500);
        builder.Property(x => x.FailureReason).HasMaxLength(500);

        builder.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId).IsRequired(false);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.UserId);
        // Audit logs are never soft-deleted — no query filter
    }
}

public class DataQualityIssueConfiguration : IEntityTypeConfiguration<DataQualityIssue>
{
    public void Configure(EntityTypeBuilder<DataQualityIssue> builder)
    {
        builder.ToTable("data_quality_issues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IssueType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Open");
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AffectedField).HasMaxLength(100);
        builder.Property(x => x.SourceSystemCode).HasMaxLength(50);
        builder.Property(x => x.SuggestedAction).HasMaxLength(500);
        builder.Property(x => x.ResolvedBy).HasMaxLength(100);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.DataQualityIssues).HasForeignKey(x => x.PropertyId);
        builder.HasIndex(x => new { x.PropertyId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("tax_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.PropertyType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Governorate).HasMaxLength(100);
        builder.Property(x => x.TaxRate).HasPrecision(6, 4);
        builder.Property(x => x.MinTaxableValue).HasPrecision(18, 2);
        builder.Property(x => x.MaxTaxableValue).HasPrecision(18, 2);
        builder.Property(x => x.FlatAmount).HasPrecision(18, 2);
        builder.Property(x => x.LegalReference).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ValuationRuleConfiguration : IEntityTypeConfiguration<ValuationRule>
{
    public void Configure(EntityTypeBuilder<ValuationRule> builder)
    {
        builder.ToTable("valuation_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.PropertyType).HasMaxLength(100);
        builder.Property(x => x.Governorate).HasMaxLength(100);
        builder.Property(x => x.DeductionPercentage).HasPrecision(5, 2);
        builder.Property(x => x.MinNetValue).HasPrecision(18, 2);
        builder.Property(x => x.MaxNetValue).HasPrecision(18, 2);
        builder.Property(x => x.StandardRentPerSqM).HasPrecision(10, 2);
        builder.Property(x => x.LegalReference).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ExemptionRuleConfiguration : IEntityTypeConfiguration<ExemptionRule>
{
    public void Configure(EntityTypeBuilder<ExemptionRule> builder)
    {
        builder.ToTable("exemption_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ExemptionType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExemptionPercentage).HasPrecision(5, 2);
        builder.Property(x => x.MaxExemptAmount).HasPrecision(18, 2);
        builder.Property(x => x.EligibilityCriteria).HasMaxLength(5000);
        builder.Property(x => x.LegalReference).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
