using Microsoft.EntityFrameworkCore;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence;

public static class IntelligenceDbContextSeed
{
    public static async Task SeedAsync(IntelligenceDbContext context)
    {
        await SeedDefaultModelsAsync(context);
    }

    // Register the rule-based duplicate detector as the default production model so
    // the nightly batch inference job can run without requiring a prior training run.
    // Risk scorer and fraud detector need a real training run before they can produce
    // predictions — this seed only covers the cold-start duplicate detection case.
    private static async Task SeedDefaultModelsAsync(IntelligenceDbContext context)
    {
        var hasDuplicateModel = await context.ModelRegistry.AnyAsync(
            m => m.PredictionType == PredictionType.DuplicateDetection
              && m.Status == ModelStatus.Production);

        if (hasDuplicateModel) return;

        context.ModelRegistry.Add(new ModelRegistration
        {
            ModelName = "duplicate_detector",
            ModelType = "RuleBased",
            Version = "v1.0-rule-based",
            Status = ModelStatus.Production,
            PredictionType = PredictionType.DuplicateDetection,
            FeatureVersion = "v1",
            ArtifactPath = "/app/models/rule-based",
            TrainedAt = DateTime.UtcNow,
            PromotedAt = DateTime.UtcNow,
            Notes = "Default rule-based duplicate detector. Replace by training and promoting an ML model.",
        });

        await context.SaveChangesAsync();
    }
}
