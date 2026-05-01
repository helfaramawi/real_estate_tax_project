using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class PredictionResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public Guid ModelId { get; set; }
    public PredictionType PredictionType { get; set; }
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
    public double Score { get; set; }
    public string? Label { get; set; }
    public double Confidence { get; set; }
    public string? Explanation { get; set; }
    public string? FeatureSnapshot { get; set; }
    public bool IsReviewed { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewOutcome { get; set; }
    public string? ReviewNotes { get; set; }

    public ModelRegistration? Model { get; set; }
}
