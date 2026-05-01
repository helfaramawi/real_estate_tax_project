using Microsoft.EntityFrameworkCore;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence;

public class IntelligenceDbContext(DbContextOptions<IntelligenceDbContext> options) : DbContext(options)
{
    public DbSet<FeatureVector> FeatureVectors => Set<FeatureVector>();
    public DbSet<ModelRegistration> ModelRegistry => Set<ModelRegistration>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();
    public DbSet<GeoCluster> GeoClusters => Set<GeoCluster>();
    public DbSet<SpatialAnomaly> SpatialAnomalies => Set<SpatialAnomaly>();
    public DbSet<InspectorTrack> InspectorTracks => Set<InspectorTrack>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<GeoFenceZone> GeoFenceZones => Set<GeoFenceZone>();
    public DbSet<OfflineSyncPacket> OfflineSyncQueue => Set<OfflineSyncPacket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("intel");
        modelBuilder.HasPostgresExtension("postgis");

        // Apply snake_case naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.GetColumnName()!));
            foreach (var key in entity.GetKeys())
                key.SetName(ToSnakeCase(key.GetName()!));
            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }

        modelBuilder.ApplyConfiguration(new FeatureVectorConfiguration());
        modelBuilder.ApplyConfiguration(new ModelRegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new PredictionResultConfiguration());
        modelBuilder.ApplyConfiguration(new GeoClusterConfiguration());
        modelBuilder.ApplyConfiguration(new SpatialAnomalyConfiguration());
        modelBuilder.ApplyConfiguration(new InspectorTrackConfiguration());
        modelBuilder.ApplyConfiguration(new RouteAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new GeoFenceZoneConfiguration());
        modelBuilder.ApplyConfiguration(new OfflineSyncPacketConfiguration());
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
