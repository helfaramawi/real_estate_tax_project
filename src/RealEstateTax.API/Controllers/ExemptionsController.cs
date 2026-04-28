using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Exemptions;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Tax exemption applications — submit, approve, and reject under Maker-Checker workflow.</summary>
[ApiController]
[Route("api/exemptions")]
[Authorize]
[Produces("application/json")]
public class ExemptionsController : ControllerBase
{
    private readonly IExemptionAppService _service;
    public ExemptionsController(IExemptionAppService service) => _service = service;

    /// <summary>Submit a new exemption application for a property.</summary>
    /// <remarks>
    /// Exemption types include SocialHousing (≤100 m²), DisabledOwner, WidowOrOrphan,
    /// Agricultural, Religious, Government, and NewConstruction (within 2 years of completion).
    /// All applications with RequiresApproval=true enter a Maker-Checker review queue.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Citizen")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Submit([FromBody] SubmitExemptionRequest request, CancellationToken ct) =>
        (await _service.SubmitAsync(request, ct)).ToActionResult(this);

    /// <summary>Get all exemptions for a specific property.</summary>
    /// <param name="propertyId">Property GUID.</param>
    [HttpGet("property/{propertyId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve an exemption application (Maker-Checker: reviewer cannot approve their own review).</summary>
    /// <param name="id">Exemption GUID.</param>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveExemptionRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Reject an exemption application with a mandatory reason.</summary>
    /// <param name="id">Exemption GUID.</param>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExemptionRequest request, CancellationToken ct) =>
        (await _service.RejectAsync(id, request, ct)).ToActionResult(this);
}
