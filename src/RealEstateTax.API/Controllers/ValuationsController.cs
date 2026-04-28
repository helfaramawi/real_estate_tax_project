using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Valuations;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Property valuations — create, approve, and reject under Maker-Checker workflow.</summary>
[ApiController]
[Route("api/valuations")]
[Authorize]
[Produces("application/json")]
public class ValuationsController : ControllerBase
{
    private readonly IValuationAppService _service;
    public ValuationsController(IValuationAppService service) => _service = service;

    /// <summary>Create a new valuation for a property (status: Draft → Submitted).</summary>
    /// <remarks>
    /// Annual Rental Value is calculated from the configured ValuationRule for the property type
    /// and governorate, applying the deduction percentage mandated by Law 196/2008.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateValuationRequest request, CancellationToken ct) =>
        (await _service.CreateAsync(request, ct)).ToActionResult(this);

    /// <summary>Get all valuations for a specific property.</summary>
    /// <param name="propertyId">Property GUID.</param>
    [HttpGet("property/{propertyId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve a valuation (Maker-Checker: approver must differ from the preparer).</summary>
    /// <param name="id">Valuation GUID.</param>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveValuationRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Reject a valuation with a mandatory reason, returning it to Draft for correction.</summary>
    /// <param name="id">Valuation GUID.</param>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectValuationRequest request, CancellationToken ct) =>
        (await _service.RejectAsync(id, request, ct)).ToActionResult(this);
}
