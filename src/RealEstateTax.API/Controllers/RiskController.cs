using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Risk;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class RiskController : ControllerBase
{
    private readonly IRiskAppService _service;
    public RiskController(IRiskAppService service) => _service = service;

    [HttpGet("api/risk/property/{propertyId:guid}")]
    public async Task<IActionResult> GetRiskScore(Guid propertyId, CancellationToken ct) =>
        (await _service.GetRiskScoreAsync(propertyId, ct)).ToActionResult(this);

    [HttpPost("api/risk/recalculate/{propertyId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    public async Task<IActionResult> Recalculate(Guid propertyId, CancellationToken ct) =>
        (await _service.RecalculateAsync(propertyId, ct)).ToActionResult(this);

    [HttpGet("api/fraud-flags")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    public async Task<IActionResult> GetFraudFlags([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetFraudFlagsAsync(query, ct)).ToActionResult(this);

    [HttpPost("api/fraud-flags")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Auditor")]
    public async Task<IActionResult> CreateFraudFlag([FromBody] CreateFraudFlagRequest request, CancellationToken ct) =>
        (await _service.CreateFraudFlagAsync(request, ct)).ToActionResult(this);
}
