using Microsoft.EntityFrameworkCore;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Taxpayer> Taxpayers { get; }
    DbSet<Property> Properties { get; }
    DbSet<PropertyUnit> PropertyUnits { get; }
    DbSet<PropertyOwnership> PropertyOwnerships { get; }
    DbSet<PropertyLocation> PropertyLocations { get; }
    DbSet<PropertySource> PropertySources { get; }
    DbSet<PropertySourceRecord> PropertySourceRecords { get; }
    DbSet<FieldSurvey> FieldSurveys { get; }
    DbSet<SurveyAttachment> SurveyAttachments { get; }
    DbSet<Valuation> Valuations { get; }
    DbSet<TaxAssessment> TaxAssessments { get; }
    DbSet<TaxBill> TaxBills { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Exemption> Exemptions { get; }
    DbSet<Appeal> Appeals { get; }
    DbSet<AppealDocument> AppealDocuments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ExternalEntity> ExternalEntities { get; }
    DbSet<IntegrationRequest> IntegrationRequests { get; }
    DbSet<RiskScore> RiskScores { get; }
    DbSet<FraudFlag> FraudFlags { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DataQualityIssue> DataQualityIssues { get; }
    DbSet<TaxRule> TaxRules { get; }
    DbSet<ValuationRule> ValuationRules { get; }
    DbSet<ExemptionRule> ExemptionRules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
