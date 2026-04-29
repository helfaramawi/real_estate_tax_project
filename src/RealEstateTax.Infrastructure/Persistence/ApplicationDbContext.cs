using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Taxpayer> Taxpayers => Set<Taxpayer>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<PropertyOwnership> PropertyOwnerships => Set<PropertyOwnership>();
    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();
    public DbSet<PropertySource> PropertySources => Set<PropertySource>();
    public DbSet<PropertySourceRecord> PropertySourceRecords => Set<PropertySourceRecord>();
    public DbSet<FieldSurvey> FieldSurveys => Set<FieldSurvey>();
    public DbSet<SurveyAttachment> SurveyAttachments => Set<SurveyAttachment>();
    public DbSet<Valuation> Valuations => Set<Valuation>();
    public DbSet<TaxAssessment> TaxAssessments => Set<TaxAssessment>();
    public DbSet<TaxBill> TaxBills => Set<TaxBill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Exemption> Exemptions => Set<Exemption>();
    public DbSet<Appeal> Appeals => Set<Appeal>();
    public DbSet<AppealDocument> AppealDocuments => Set<AppealDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ExternalEntity> ExternalEntities => Set<ExternalEntity>();
    public DbSet<IntegrationRequest> IntegrationRequests => Set<IntegrationRequest>();
    public DbSet<RiskScore> RiskScores => Set<RiskScore>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DataQualityIssue> DataQualityIssues => Set<DataQualityIssue>();
    public DbSet<TaxRule> TaxRules => Set<TaxRule>();
    public DbSet<ValuationRule> ValuationRules => Set<ValuationRule>();
    public DbSet<ExemptionRule> ExemptionRules => Set<ExemptionRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");
        // DomainEvent is discovered via BaseEntity.DomainEvents — ignore it, it is not a DB table
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Map all column names to snake_case to match the PostgreSQL schema.
        // Table names are already set explicitly in each IEntityTypeConfiguration.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var current = property.GetColumnBaseName();
                if (current != null)
                    property.SetColumnName(ToSnakeCase(current));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append('_');
            sb.Append(char.ToLower(name[i]));
        }
        return sb.ToString();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = _currentUser.Username ?? "System";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    break;

                case EntityState.Deleted:
                    // Enforce soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = user;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
