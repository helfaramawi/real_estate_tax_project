using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class ModelRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ModelName { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ModelStatus Status { get; set; } = ModelStatus.Staged;
    public PredictionType PredictionType { get; set; }
    public string? ArtifactPath { get; set; }
    public string? MlflowRunId { get; set; }
    public string? FeatureVersion { get; set; }
    public DateTime? TrainedAt { get; set; }
    public DateTime? PromotedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public int? TrainingSampleSize { get; set; }
    public string? Metrics { get; set; }
    public string? FeatureImportance { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PredictionResult> Predictions { get; set; } = [];
}
