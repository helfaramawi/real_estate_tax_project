using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Infrastructure.BackgroundJobs;
using RealEstateTax.Intelligence.Infrastructure.MLClient;
using RealEstateTax.Intelligence.Infrastructure.Persistence;
using RealEstateTax.Intelligence.Infrastructure.Services;
using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence;

public static class IntelligenceDependencyInjection
{
    public static IServiceCollection AddIntelligence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ── Feature Management ────────────────────────────────────────────────
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));

        // ── Intelligence DbContext (same DB, intel schema) ───────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");

        services.AddDbContext<IntelligenceDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.EnableRetryOnFailure(3);
            }));

        // ── ML HTTP Client (typed, with resilience) ───────────────────────────
        var mlServiceUrl = configuration["MLService:BaseUrl"] ?? "http://ml_service:8001";

        services.AddHttpClient<IMLModelClient, MLModelHttpClient>(client =>
        {
            client.BaseAddress = new Uri(mlServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("X-Internal-Secret",
                configuration["MLService:InternalSecret"] ?? "dev-secret");
        })
        .AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = 3;
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            o.CircuitBreaker.FailureRatio = 0.5;
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60); // must be >= 2x AttemptTimeout
        });

        // ── Services ─────────────────────────────────────────────────────────
        services.AddScoped<IFeatureStoreService, FeatureStoreService>();
        services.AddScoped<IGeoIntelligenceService, GeoIntelligenceService>();
        services.AddScoped<IOfflineSyncService, OfflineSyncService>();
        services.AddScoped<IIntelligenceService, IntelligenceService>();
        services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();

        // ── Background Jobs ───────────────────────────────────────────────────
        services.AddScoped<FeatureComputationJob>();
        services.AddScoped<MLInferenceJob>();
        services.AddScoped<GeoClusteringJob>();
        services.AddScoped<OfflineSyncProcessingJob>();

        return services;
    }

    public static void RegisterIntelligenceRecurringJobs()
    {
        // Feature computation — nightly 02:00 UTC
        RecurringJob.AddOrUpdate<FeatureComputationJob>(
            "intel-feature-computation",
            j => j.ComputeAllFeaturesAsync(CancellationToken.None),
            "0 2 * * *");

        // Batch ML inference — 04:00 UTC (after features are ready)
        RecurringJob.AddOrUpdate<MLInferenceJob>(
            "intel-ml-inference-risk",
            j => j.RunBatchInferenceAsync(PredictionType.RiskScore, CancellationToken.None),
            "0 4 * * *");

        RecurringJob.AddOrUpdate<MLInferenceJob>(
            "intel-ml-inference-fraud",
            j => j.RunBatchInferenceAsync(PredictionType.FraudProbability, CancellationToken.None),
            "30 4 * * *");

        RecurringJob.AddOrUpdate<MLInferenceJob>(
            "intel-ml-inference-duplicate",
            j => j.RunBatchInferenceAsync(PredictionType.DuplicateDetection, CancellationToken.None),
            "0 5 * * *");

        // Geospatial clustering — weekly Sunday 01:00 UTC
        RecurringJob.AddOrUpdate<GeoClusteringJob>(
            "intel-geo-clustering",
            j => j.RunClusteringAsync(CancellationToken.None),
            "0 1 * * 0");

        // Geo-fence update — nightly 03:00 UTC
        RecurringJob.AddOrUpdate<GeoClusteringJob>(
            "intel-geofence-update",
            j => j.UpdateGeoFenceMembershipAsync(CancellationToken.None),
            "0 3 * * *");

        // Offline sync processing — every 5 minutes
        RecurringJob.AddOrUpdate<OfflineSyncProcessingJob>(
            "intel-offline-sync",
            j => j.ProcessPendingPacketsAsync(CancellationToken.None),
            "*/5 * * * *");
    }
}
