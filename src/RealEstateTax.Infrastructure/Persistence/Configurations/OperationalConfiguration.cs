using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Persistence.Configurations;

public class PropertySourceConfiguration : IEntityTypeConfiguration<PropertySource>
{
    public void Configure(EntityTypeBuilder<PropertySource> builder)
    {
        builder.ToTable("property_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ApiEndpoint).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PropertySourceRecordConfiguration : IEntityTypeConfiguration<PropertySourceRecord>
{
    public void Configure(EntityTypeBuilder<PropertySourceRecord> builder)
    {
        builder.ToTable("property_source_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceReferenceId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OwnerName).HasMaxLength(200);
        builder.Property(x => x.OwnerNationalId).HasMaxLength(14);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.MeterNumber).HasMaxLength(100);
        builder.Property(x => x.AccountNumber).HasMaxLength(100);
        builder.Property(x => x.PropertyTypeFromSource).HasMaxLength(100);
        builder.Property(x => x.MatchMethod).HasMaxLength(100);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.ImportBatchId).HasMaxLength(50);
        builder.Property(x => x.AreaFromSource).HasPrecision(10, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Source).WithMany(s => s.Records).HasForeignKey(x => x.SourceId);
        builder.HasOne(x => x.MasterProperty).WithMany(p => p.SourceRecords).HasForeignKey(x => x.MasterPropertyId).IsRequired(false);

        builder.HasIndex(x => new { x.SourceId, x.SourceReferenceId }).IsUnique();
        builder.HasIndex(x => x.IsMatched);
        builder.HasIndex(x => x.OwnerNationalId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class FieldSurveyConfiguration : IEntityTypeConfiguration<FieldSurvey>
{
    public void Configure(EntityTypeBuilder<FieldSurvey> builder)
    {
        builder.ToTable("field_surveys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SurveyCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FieldNotes).HasMaxLength(5000);
        builder.Property(x => x.ObservedCondition).HasMaxLength(200);
        builder.Property(x => x.OccupantInfo).HasMaxLength(500);
        builder.Property(x => x.PropertyTypeObserved).HasMaxLength(100);
        builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
        builder.Property(x => x.ReviewedBy).HasMaxLength(100);
        builder.Property(x => x.MeasuredArea).HasPrecision(10, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.FieldSurveys).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId);

        builder.HasIndex(x => x.SurveyCode).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SurveyAttachmentConfiguration : IEntityTypeConfiguration<SurveyAttachment>
{
    public void Configure(EntityTypeBuilder<SurveyAttachment> builder)
    {
        builder.ToTable("survey_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AttachmentType).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Survey).WithMany(s => s.Attachments).HasForeignKey(x => x.SurveyId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ValuationConfiguration : IEntityTypeConfiguration<Valuation>
{
    public void Configure(EntityTypeBuilder<Valuation> builder)
    {
        builder.ToTable("valuations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValuationCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.GrossValue).HasPrecision(18, 2);
        builder.Property(x => x.NetValue).HasPrecision(18, 2);
        builder.Property(x => x.TotalArea).HasPrecision(12, 2);
        builder.Property(x => x.RentableArea).HasPrecision(12, 2);
        builder.Property(x => x.AnnualRentalValue).HasPrecision(18, 2);
        builder.Property(x => x.MarketValuePerSqM).HasPrecision(12, 2);
        builder.Property(x => x.DeductionPercentage).HasPrecision(5, 2);
        builder.Property(x => x.CapitalizationRate).HasPrecision(6, 4);
        builder.Property(x => x.ApprovalNotes).HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.PreparedBy).HasMaxLength(100);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.Valuations).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.ValuationRule).WithMany().HasForeignKey(x => x.ValuationRuleId).IsRequired(false);

        builder.HasIndex(x => x.ValuationCode).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.TaxYear });
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();
    }
}

public class TaxAssessmentConfiguration : IEntityTypeConfiguration<TaxAssessment>
{
    public void Configure(EntityTypeBuilder<TaxAssessment> builder)
    {
        builder.ToTable("tax_assessments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssessmentCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TaxableValue).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(6, 4);
        builder.Property(x => x.GrossTaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExemptionAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetTaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Penalties).HasPrecision(18, 2);
        builder.Property(x => x.LateFees).HasPrecision(18, 2);
        builder.Property(x => x.TotalDue).HasPrecision(18, 2);
        builder.Property(x => x.PreparedBy).HasMaxLength(100);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);
        builder.Property(x => x.ApprovalNotes).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.TaxAssessments).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.Valuation).WithMany(v => v.TaxAssessments).HasForeignKey(x => x.ValuationId);
        builder.HasOne(x => x.TaxRule).WithMany().HasForeignKey(x => x.TaxRuleId).IsRequired(false);
        builder.HasOne(x => x.Exemption).WithMany().HasForeignKey(x => x.ExemptionId).IsRequired(false);

        builder.HasIndex(x => x.AssessmentCode).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.TaxYear });
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();
    }
}

public class TaxBillConfiguration : IEntityTypeConfiguration<TaxBill>
{
    public void Configure(EntityTypeBuilder<TaxBill> builder)
    {
        builder.ToTable("tax_bills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BillNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
        builder.Property(x => x.IssuedBy).HasMaxLength(100);
        builder.Property(x => x.CancelledBy).HasMaxLength(100);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Ignore(x => x.RemainingAmount);

        builder.HasOne(x => x.Property).WithMany(p => p.TaxBills).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.TaxAssessment).WithMany(a => a.TaxBills).HasForeignKey(x => x.TaxAssessmentId);
        builder.HasOne(x => x.Taxpayer).WithMany().HasForeignKey(x => x.TaxpayerId);

        builder.HasIndex(x => x.BillNumber).IsUnique();
        builder.HasIndex(x => new { x.PropertyId, x.TaxYear });
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PaymentReference).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.FeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("EGP");
        builder.Property(x => x.ExternalTransactionId).HasMaxLength(200);
        builder.Property(x => x.BankReference).HasMaxLength(200);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(100);
        builder.Property(x => x.CollectedBy).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.RefundReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.TaxBill).WithMany(b => b.Payments).HasForeignKey(x => x.TaxBillId);
        builder.HasOne(x => x.Taxpayer).WithMany().HasForeignKey(x => x.TaxpayerId);

        builder.HasIndex(x => x.PaymentReference).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
