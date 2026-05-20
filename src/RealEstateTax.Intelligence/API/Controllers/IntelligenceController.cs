using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Enums;
using System.Security.Claims;

namespace RealEstateTax.Intelligence.API.Controllers;

/// <summary>AI/ML prediction management and model governance.</summary>
[ApiController]
[Route("api/v2/intelligence")]
[Authorize]
[Produces("application/json")]
public class IntelligenceController(IIntelligenceService intelligenceService) : ControllerBase
{
    /// <summary>Get all ML predictions for a property.</summary>
    [HttpGet("properties/{propertyId:guid}/predictions")]
    [FeatureGate("MLRiskScoring")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetPredictions(Guid propertyId, CancellationToken ct)
    {
        var predictions = await intelligenceService.GetPredictionsAsync(propertyId, ct);
        return Ok(new { success = true, data = predictions });
    }

    /// <summary>Trigger on-demand ML predictions for a property.</summary>
    [HttpPost("properties/{propertyId:guid}/predict")]
    [FeatureGate("MLRiskScoring")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> TriggerPrediction(
        Guid propertyId,
        [FromBody] TriggerPredictionRequest request,
        CancellationToken ct)
    {
        var types = request.PredictionTypes
            .Select(t => Enum.TryParse<PredictionType>(t, true, out var p) ? (PredictionType?)p : null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();

        if (types.Count == 0)
            return BadRequest(new { success = false, error = "No valid prediction types specified" });

        await intelligenceService.TriggerPredictionAsync(propertyId, types, ct);
        return Accepted(new { success = true, message = "Prediction triggered" });
    }

    /// <summary>Get predictions pending human review.</summary>
    [HttpGet("predictions/pending-review")]
    [FeatureGate("MLRiskScoring")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetPendingReview(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] double? minScore = null,
        CancellationToken ct = default)
    {
        PredictionType? predType = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<PredictionType>(type, true, out var parsed))
            predType = parsed;

        var result = await intelligenceService.GetPendingReviewAsync(page, pageSize, predType, minScore, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Submit a review outcome for an ML prediction.</summary>
    [HttpPatch("predictions/{predictionId:guid}/review")]
    [FeatureGate("MLRiskScoring")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> ReviewPrediction(
        Guid predictionId,
        [FromBody] ReviewPredictionRequest request,
        CancellationToken ct)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var result = await intelligenceService.ReviewPredictionAsync(predictionId, request.Outcome, request.Notes, userId, ct);
        if (result is null) return NotFound(new { success = false, error = "Prediction not found" });
        return Ok(new { success = true, data = result });
    }

    /// <summary>Dashboard summary of all AI/ML intelligence metrics.</summary>
    [HttpGet("dashboard/summary")]
    [FeatureGate("MLRiskScoring")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var summary = await intelligenceService.GetDashboardSummaryAsync(ct);
        return Ok(new { success = true, data = summary });
    }

    /// <summary>List all registered ML models.</summary>
    [HttpGet("models")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetModels(CancellationToken ct)
    {
        var models = await intelligenceService.GetModelsAsync(ct);
        return Ok(new { success = true, data = models });
    }

    /// <summary>Promote a Staged model to Production.</summary>
    [HttpPost("models/{modelId:guid}/promote")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> PromoteModel(Guid modelId, CancellationToken ct)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var model = await intelligenceService.PromoteModelAsync(modelId, userId, ct);
        if (model is null) return BadRequest(new { success = false, error = "Model not found or not in Staged status" });
        return Ok(new { success = true, data = model });
    }
}

public record TriggerPredictionRequest(List<string> PredictionTypes);
public record ReviewPredictionRequest(string Outcome, string? Notes);
