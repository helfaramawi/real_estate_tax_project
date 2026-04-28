using System.Text.Json;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services;

/// <summary>
/// Evaluates exemption eligibility and calculates exemption amounts.
/// TODO: Map all eligibility criteria to specific articles of Egyptian Real Estate Tax Law 196/2008.
/// TODO: Obtain official exemption thresholds and rate tables from the Egyptian Tax Authority.
/// </summary>
public class ExemptionDomainService : IExemptionService
{
    public Task<EligibilityResult> CheckEligibilityAsync(
        Property property,
        Taxpayer taxpayer,
        ExemptionRule rule,
        CancellationToken ct = default)
    {
        var satisfied = new List<string>();
        var failed = new List<string>();

        // Rule must be active
        if (!rule.IsActive)
        {
            failed.Add("ExemptionRule is inactive");
            return Task.FromResult(new EligibilityResult(false, [], [.. failed], "Contact tax authority to apply for a current exemption rule."));
        }

        // Check expiry if configured
        if (rule.MaxDurationMonths.HasValue && property.CreatedAt.AddMonths(rule.MaxDurationMonths.Value) < DateTime.UtcNow)
        {
            failed.Add($"MaxDurationMonths ({rule.MaxDurationMonths}) exceeded");
        }
        else
        {
            satisfied.Add("WithinMaxDuration");
        }

        // Evaluate based on exemption type
        switch (rule.ExemptionType)
        {
            case nameof(ExemptionType.SocialHousing):
                EvaluateSocialHousing(property, satisfied, failed);
                break;

            case nameof(ExemptionType.DisabledOwner):
                EvaluateDisabledOwner(taxpayer, satisfied, failed);
                break;

            case nameof(ExemptionType.WidowOrOrphan):
                EvaluateWidowOrOrphan(taxpayer, satisfied, failed);
                break;

            case nameof(ExemptionType.Agricultural):
                EvaluateAgricultural(property, satisfied, failed);
                break;

            case nameof(ExemptionType.ReligiousInstitution):
                EvaluateReligious(property, taxpayer, satisfied, failed);
                break;

            case nameof(ExemptionType.GovernmentOwned):
                EvaluateGovernment(taxpayer, satisfied, failed);
                break;

            case nameof(ExemptionType.NewConstruction):
                EvaluateNewConstruction(property, satisfied, failed);
                break;

            default:
                // JSON-encoded eligibility criteria
                if (!string.IsNullOrEmpty(rule.EligibilityCriteria))
                    EvaluateJsonCriteria(rule.EligibilityCriteria, property, taxpayer, satisfied, failed);
                else
                    satisfied.Add("NoSpecificCriteriaConfigured");
                break;
        }

        var isEligible = failed.Count == 0;
        var recommendation = isEligible
            ? null
            : $"Eligibility failed: {string.Join("; ", failed)}. Contact tax authority for guidance.";

        return Task.FromResult(new EligibilityResult(isEligible, [.. satisfied], [.. failed], recommendation));
    }

    public Task<decimal> CalculateExemptionAmountAsync(
        decimal grossTaxAmount,
        Exemption exemption,
        CancellationToken ct = default)
    {
        if (exemption.Status != ExemptionStatus.Approved)
            return Task.FromResult(0m);

        decimal amount;
        if (exemption.ExemptionPercentage.HasValue)
            amount = grossTaxAmount * (exemption.ExemptionPercentage.Value / 100m);
        else if (exemption.ExemptAmount.HasValue)
            amount = exemption.ExemptAmount.Value;
        else
            return Task.FromResult(0m);

        // Cap at 100% of gross tax
        return Task.FromResult(Math.Min(amount, grossTaxAmount));
    }

    // ── Type-specific eligibility checks ──────────────────────────────────────

    private static void EvaluateSocialHousing(Property property, List<string> sat, List<string> fail)
    {
        // TODO: Verify social housing area threshold per Egyptian law (typically ≤100 m²)
        if (property.BuiltUpArea <= 100m)
            sat.Add("BuiltUpArea≤100m²(SocialHousingThreshold)");
        else
            fail.Add($"BuiltUpArea={property.BuiltUpArea}m²ExceedsSocialHousingLimit(100m²)");

        if (property.Type == PropertyType.Residential)
            sat.Add("PropertyTypeIsResidential");
        else
            fail.Add($"PropertyType={property.Type}IsNotResidential");
    }

    private static void EvaluateDisabledOwner(Taxpayer taxpayer, List<string> sat, List<string> fail)
    {
        // TODO: Integrate with government disability registry (Ministry of Social Solidarity)
        if (!taxpayer.IsCorporate)
            sat.Add("OwnerIsIndividual");
        else
            fail.Add("CorporateEntitiesIneligibleForDisabilityExemption");
        // Note: actual disability verification requires external registry lookup
        sat.Add("DisabilityVerificationPending(ManualReview)");
    }

    private static void EvaluateWidowOrOrphan(Taxpayer taxpayer, List<string> sat, List<string> fail)
    {
        // TODO: Integrate with civil registry to verify widow/orphan status
        if (!taxpayer.IsCorporate)
            sat.Add("OwnerIsIndividual");
        else
            fail.Add("CorporateEntitiesIneligibleForWidow/OrphanExemption");
        sat.Add("StatusVerificationPending(ManualReview)");
    }

    private static void EvaluateAgricultural(Property property, List<string> sat, List<string> fail)
    {
        if (property.Type == PropertyType.Agricultural)
            sat.Add("PropertyTypeIsAgricultural");
        else
            fail.Add($"PropertyType={property.Type}IsNotAgricultural");
    }

    private static void EvaluateReligious(Property property, Taxpayer taxpayer, List<string> sat, List<string> fail)
    {
        if (property.Type == PropertyType.Religious)
            sat.Add("PropertyTypeIsReligious");
        else
            fail.Add($"PropertyType={property.Type}IsNotReligious");

        if (taxpayer.IsCorporate)
            sat.Add("OwnerIsCorporateEntity");
        else
            fail.Add("ReligiousExemptionRequiresCorporateRegistration");
    }

    private static void EvaluateGovernment(Taxpayer taxpayer, List<string> sat, List<string> fail)
    {
        // TODO: Validate against official list of government entities
        if (taxpayer.IsCorporate)
            sat.Add("OwnerIsCorporate(GovernmentEntityVerificationPending)");
        else
            fail.Add("GovernmentExemptionRequiresCorporateRegistration");
    }

    private static void EvaluateNewConstruction(Property property, List<string> sat, List<string> fail)
    {
        // TODO: Verify grace period per Egyptian law (Law 196/2008 Art. X)
        if (property.YearBuilt.HasValue && property.YearBuilt.Value >= DateTime.UtcNow.Year - 2)
            sat.Add($"ConstructedWithin2Years(YearBuilt={property.YearBuilt})");
        else
            fail.Add("PropertyNotWithinNewConstructionGracePeriod(2years)");
    }

    private static void EvaluateJsonCriteria(
        string criteriaJson, Property property, Taxpayer taxpayer,
        List<string> sat, List<string> fail)
    {
        try
        {
            var criteria = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(criteriaJson);
            if (criteria == null) return;

            if (criteria.TryGetValue("maxBuiltUpArea", out var maxArea))
            {
                var limit = maxArea.GetDecimal();
                if (property.BuiltUpArea <= limit)
                    sat.Add($"BuiltUpArea≤{limit}m²");
                else
                    fail.Add($"BuiltUpArea={property.BuiltUpArea}m²Exceeds{limit}m²");
            }

            if (criteria.TryGetValue("requiredPropertyType", out var reqType) &&
                Enum.TryParse<PropertyType>(reqType.GetString(), out var required))
            {
                if (property.Type == required)
                    sat.Add($"PropertyType={required}");
                else
                    fail.Add($"PropertyType={property.Type}≠Required({required})");
            }
        }
        catch
        {
            fail.Add("InvalidEligibilityCriteriaConfiguration");
        }
    }
}
