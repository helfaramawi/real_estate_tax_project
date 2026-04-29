using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Risk;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Risk scoring and fraud flag management.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class RiskController : ControllerBase
{
    private readonly IRiskAppService _service;
    public RiskController(IRiskAppService service) => _service = service;

    /// <summary>Get the most recent risk score for a property.</summary>
    /// <param name="propertyId">Property GUID.</param>
    /// <remarks>
    /// Risk score (0–100) is composed of 6 weighted factors: DataCompleteness,
    /// ValuationConsistency, OwnershipChain, PaymentHistory, GeoVerification,
    /// and SourceConsistency. Levels: Low (&lt;25), Medium (25–49), High (50–74), Critical (≥75).
    /// </remarks>
    [HttpGet("api/risk/property/{propertyId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRiskScore(Guid propertyId, CancellationToken ct) =>
        (await _service.GetRiskScoreAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Trigger an on-demand risk score recalculation for a property (enqueues a Hangfire job).</summary>
    /// <param name="propertyId">Property GUID.</param>
    [HttpPost("api/risk/recalculate/{propertyId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Recalculate(Guid propertyId, CancellationToken ct) =>
        (await _service.RecalculateAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>List all open fraud flags with optional filtering and pagination.</summary>
    [HttpGet("api/fraud-flags")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetFraudFlags([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetFraudFlagsAsync(query, ct)).ToActionResult(this);

    /// <summary>Raise a new fraud flag against a property.</summary>
    /// <remarks>
    /// Flag types: DuplicateProperty, OwnershipFraud, UnderReportedValue, FakeDocuments, Other.
    /// Severity maps to the RiskLevel enum: Low, Medium, High, Critical.
    /// </remarks>
    [HttpPost("api/fraud-flags")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateFraudFlag([FromBody] CreateFraudFlagRequest request, CancellationToken ct) =>
        (await _service.CreateFraudFlagAsync(request, ct)).ToActionResult(this);
}
