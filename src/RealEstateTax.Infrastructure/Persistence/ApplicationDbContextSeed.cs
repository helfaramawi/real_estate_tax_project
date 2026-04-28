using Microsoft.EntityFrameworkCore;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedPermissionsAsync(context);
        await SeedAdminUserAsync(context);
        await SeedRulesAsync(context);
        await SeedExternalEntitiesAsync(context);
        await SeedPropertySourcesAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var roles = new[]
        {
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000001"), Name = "SuperAdmin",       Description = "Full system access",                        IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000002"), Name = "Admin",            Description = "Administrative access",                     IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000003"), Name = "TaxOfficer",       Description = "Tax assessment and billing operations",      IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000004"), Name = "ValuationOfficer", Description = "Property valuation operations",             IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000005"), Name = "FieldInspector",   Description = "Field survey and property inspection",      IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000006"), Name = "AppealsOfficer",   Description = "Appeals and objections processing",         IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000007"), Name = "CollectionOfficer", Description = "Payment collection operations",           IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000008"), Name = "Auditor",          Description = "Read-only audit and reporting access",      IsSystem = true, CreatedBy = "Seed" },
            new Role { Id = Guid.Parse("10000001-0000-0000-0000-000000000009"), Name = "Citizen",          Description = "Self-service taxpayer portal access",       IsSystem = true, CreatedBy = "Seed" },
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var resources = new[]
        {
            ("properties",    new[] { "read", "create", "update", "delete", "verify", "link-owner" }),
            ("taxpayers",     new[] { "read", "create", "update", "delete" }),
            ("valuations",    new[] { "read", "create", "approve" }),
            ("assessments",   new[] { "read", "create", "approve" }),
            ("bills",         new[] { "read", "create", "issue", "cancel" }),
            ("payments",      new[] { "read", "create" }),
            ("appeals",       new[] { "read", "create", "assign", "decide" }),
            ("exemptions",    new[] { "read", "create", "approve" }),
            ("field-surveys", new[] { "read", "create", "submit", "review" }),
            ("risk",          new[] { "read", "recalculate" }),
            ("fraud-flags",   new[] { "read", "create", "investigate" }),
            ("integrations",  new[] { "read", "receive", "retry" }),
            ("audit-logs",    new[] { "read" }),
            ("dashboard",     new[] { "read" }),
        };

        var permissions = resources
            .SelectMany(r => r.Item2.Select(a => new Permission
            {
                Id = Guid.NewGuid(),
                Resource = r.Item1,
                Action = a,
                Name = $"{r.Item1}:{a}",
                CreatedBy = "Seed"
            }))
            .ToList();

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Username == "superadmin")) return;

        var adminUser = new User
        {
            Id = Guid.Parse("20000001-0000-0000-0000-000000000001"),
            Username = "superadmin",
            Email = "superadmin@retax.gov.eg",
            FirstName = "Super",
            LastName = "Admin",
            // Default password: Admin@12345 — CHANGE IN PRODUCTION
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
            IsActive = true,
            IsEmailVerified = true,
            CreatedBy = "Seed"
        };

        await context.Users.AddAsync(adminUser);

        var superAdminRole = await context.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = superAdminRole.Id,
            CreatedBy = "Seed"
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRulesAsync(ApplicationDbContext context)
    {
        if (await context.TaxRules.AnyAsync()) return;

        // TODO: Replace placeholder rates with verified values from Egyptian Real Estate Tax Law 196/2008 and Ministry of Finance decrees
        await context.TaxRules.AddAsync(new TaxRule
        {
            Code = "EGY-RES-DEFAULT",
            Name = "Egypt Residential Default Tax Rate",
            PropertyType = "Residential",
            TaxRate = 10.0m,            // TODO: VERIFY — placeholder 10%
            MinTaxableValue = 24000m,   // TODO: VERIFY — annual rental value threshold
            IsActive = true,
            LegalReference = "Law 196/2008 — TODO: verify article reference",
            CreatedBy = "Seed"
        });

        await context.TaxRules.AddAsync(new TaxRule
        {
            Code = "EGY-COM-DEFAULT",
            Name = "Egypt Commercial Default Tax Rate",
            PropertyType = "Commercial",
            TaxRate = 10.0m,            // TODO: VERIFY
            IsActive = true,
            LegalReference = "Law 196/2008 — TODO: verify article reference",
            CreatedBy = "Seed"
        });

        await context.ValuationRules.AddAsync(new ValuationRule
        {
            Code = "EGY-RENTAL-DEFAULT",
            Name = "Egypt Standard Rental Value Deduction",
            DeductionPercentage = 30.0m,   // TODO: VERIFY — placeholder 30% maintenance deduction
            UseRentalValue = true,
            IsActive = true,
            LegalReference = "Law 196/2008 — TODO: verify deduction article",
            CreatedBy = "Seed"
        });

        await context.ExemptionRules.AddRangeAsync(new[]
        {
            new ExemptionRule { Code = "EGY-AGR-FULL",  Name = "Agricultural Land Full Exemption",  ExemptionType = "Agricultural",      IsFullExemption = true,  RequiresApproval = true, IsActive = true, LegalReference = "Law 196/2008 Art. TODO", CreatedBy = "Seed" },
            new ExemptionRule { Code = "EGY-REL-FULL",  Name = "Religious Property Full Exemption", ExemptionType = "Religious",         IsFullExemption = true,  RequiresApproval = true, IsActive = true, LegalReference = "Law 196/2008 Art. TODO", CreatedBy = "Seed" },
            new ExemptionRule { Code = "EGY-GOV-FULL",  Name = "Government Property Exemption",     ExemptionType = "GovernmentOwned",   IsFullExemption = true,  RequiresApproval = false, IsActive = true, LegalReference = "Law 196/2008 Art. TODO", CreatedBy = "Seed" },
            new ExemptionRule { Code = "EGY-LOW-PART",  Name = "Low-Income Household Exemption",    ExemptionType = "LowIncome",         ExemptionPercentage = 100m, RequiresApproval = true, IsActive = true, LegalReference = "Law 196/2008 Art. TODO", CreatedBy = "Seed" },
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedExternalEntitiesAsync(ApplicationDbContext context)
    {
        if (await context.ExternalEntities.AnyAsync()) return;

        await context.ExternalEntities.AddRangeAsync(new[]
        {
            new ExternalEntity { Code = "NWCO",      Name = "National Water Company",         IsActive = true, CreatedBy = "Seed" },
            new ExternalEntity { Code = "GECOL",     Name = "General Electric Company",       IsActive = true, CreatedBy = "Seed" },
            new ExternalEntity { Code = "CADASTER",  Name = "Egyptian Cadastral Registry",    IsActive = true, CreatedBy = "Seed" },
            new ExternalEntity { Code = "NOTARY",    Name = "National Notarization Authority",IsActive = true, CreatedBy = "Seed" },
            new ExternalEntity { Code = "NID",       Name = "National ID Database",           IsActive = true, CreatedBy = "Seed" },
            new ExternalEntity { Code = "COMM_REG",  Name = "Commercial Registry",            IsActive = true, CreatedBy = "Seed" },
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedPropertySourcesAsync(ApplicationDbContext context)
    {
        if (await context.PropertySources.AnyAsync()) return;

        await context.PropertySources.AddRangeAsync(new[]
        {
            new PropertySource { Code = "ELEC",     Name = "Electricity Utility Records",   SourceSystem = SourceSystem.ElectricityUtility, IsActive = true, CreatedBy = "Seed" },
            new PropertySource { Code = "WATER",    Name = "Water Utility Records",         SourceSystem = SourceSystem.WaterUtility,       IsActive = true, CreatedBy = "Seed" },
            new PropertySource { Code = "CADASTER", Name = "Cadastral Survey Records",      SourceSystem = SourceSystem.CadastralRegistry,  IsActive = true, CreatedBy = "Seed" },
            new PropertySource { Code = "MUNI",     Name = "Municipal Records",             SourceSystem = SourceSystem.MunicipalRecords,   IsActive = true, CreatedBy = "Seed" },
            new PropertySource { Code = "SURVEY",   Name = "Field Survey Data",             SourceSystem = SourceSystem.FieldSurvey,        IsActive = true, CreatedBy = "Seed" },
            new PropertySource { Code = "RE_REG",   Name = "Real Estate Registry",          SourceSystem = SourceSystem.RealEstateRegistry, IsActive = true, CreatedBy = "Seed" },
        });

        await context.SaveChangesAsync();
    }
}
