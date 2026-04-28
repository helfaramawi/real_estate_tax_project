using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Persistence.Configurations;

public class TaxpayerConfiguration : IEntityTypeConfiguration<Taxpayer>
{
    public void Configure(EntityTypeBuilder<Taxpayer> builder)
    {
        builder.ToTable("taxpayers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaxpayerCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NationalId).HasMaxLength(14).IsRequired();
        builder.Property(x => x.TaxRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.CommercialRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FatherName).HasMaxLength(100);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.LegalForm).HasMaxLength(100);
        builder.Property(x => x.Gender).HasMaxLength(10);
        builder.Property(x => x.Nationality).HasMaxLength(10).HasDefaultValue("EGY");
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.AlternativePhone).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Governorate).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.TaxpayerCode).IsUnique();
        builder.HasIndex(x => x.NationalId);
        builder.HasIndex(x => x.TaxRegistrationNumber);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();

        builder.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).IsRequired(false);
    }
}

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PropertyCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ParcelNumber).HasMaxLength(50);
        builder.Property(x => x.CadastralReference).HasMaxLength(100);
        builder.Property(x => x.SubType).HasMaxLength(100);
        builder.Property(x => x.UsageDescription).HasMaxLength(500);
        builder.Property(x => x.BuildingCondition).HasMaxLength(100);
        builder.Property(x => x.StreetAddress).HasMaxLength(300);
        builder.Property(x => x.BuildingNumber).HasMaxLength(20);
        builder.Property(x => x.Neighbourhood).HasMaxLength(100);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Governorate).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.FullAddress).HasMaxLength(600);
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.VerificationNotes).HasMaxLength(1000);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);
        builder.Property(x => x.BuiltUpArea).HasPrecision(12, 2);
        builder.Property(x => x.LandArea).HasPrecision(12, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.PropertyCode).IsUnique();
        builder.HasIndex(x => x.ParcelNumber);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Governorate);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.UseXminAsConcurrencyToken();

        builder.HasOne(x => x.Location).WithOne(l => l.Property).HasForeignKey<PropertyLocation>(l => l.PropertyId);
    }
}

public class PropertyLocationConfiguration : IEntityTypeConfiguration<PropertyLocation>
{
    public void Configure(EntityTypeBuilder<PropertyLocation> builder)
    {
        builder.ToTable("property_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Governorate).HasMaxLength(100);
        builder.Property(x => x.Markaz).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Village).HasMaxLength(100);
        builder.Property(x => x.Neighbourhood).HasMaxLength(100);
        builder.Property(x => x.Block).HasMaxLength(50);
        builder.Property(x => x.Plot).HasMaxLength(50);
        builder.Property(x => x.GeocodingSource).HasMaxLength(50);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        // PostGIS columns — store as geometry(Point, 4326) and geometry(Polygon, 4326)
        builder.Property(x => x.Coordinates).HasColumnType("geometry(Point, 4326)");
        builder.Property(x => x.Boundary).HasColumnType("geometry(Polygon, 4326)");

        builder.HasIndex(x => x.Coordinates).HasMethod("gist");
        builder.HasIndex(x => x.Boundary).HasMethod("gist");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PropertyOwnershipConfiguration : IEntityTypeConfiguration<PropertyOwnership>
{
    public void Configure(EntityTypeBuilder<PropertyOwnership> builder)
    {
        builder.ToTable("property_ownerships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TitleDeedNumber).HasMaxLength(100);
        builder.Property(x => x.RegistrationAuthority).HasMaxLength(200);
        builder.Property(x => x.NotarizationReference).HasMaxLength(100);
        builder.Property(x => x.InheritanceReference).HasMaxLength(100);
        builder.Property(x => x.OwnershipPercentage).HasPrecision(5, 2);
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.Ownerships).HasForeignKey(x => x.PropertyId);
        builder.HasOne(x => x.Taxpayer).WithMany(t => t.PropertyOwnerships).HasForeignKey(x => x.TaxpayerId);
        builder.HasOne(x => x.PropertyUnit).WithMany(u => u.Ownerships).HasForeignKey(x => x.PropertyUnitId).IsRequired(false);

        builder.HasIndex(x => new { x.PropertyId, x.TaxpayerId, x.IsCurrent });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PropertyUnitConfiguration : IEntityTypeConfiguration<PropertyUnit>
{
    public void Configure(EntityTypeBuilder<PropertyUnit> builder)
    {
        builder.ToTable("property_units");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UnitNumber).HasMaxLength(20);
        builder.Property(x => x.UnitType).HasMaxLength(100);
        builder.Property(x => x.UsageType).HasMaxLength(100);
        builder.Property(x => x.Area).HasPrecision(10, 2);
        builder.Property(x => x.MonthlyRent).HasPrecision(12, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne(x => x.Property).WithMany(p => p.Units).HasForeignKey(x => x.PropertyId);
        builder.HasIndex(x => x.UnitCode).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
