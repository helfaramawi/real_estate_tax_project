using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Valuations;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/valuations")]
[Authorize]
[Produces("application/json")]
public class ValuationsController : ControllerBase
{
    private readonly IValuationAppService _service;
    public ValuationsController(IValuationAppService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    public async Task<IActionResult> Create([FromBody] CreateValuationRequest request, CancellationToken ct) =>
        (await _service.CreateAsync(request, ct)).ToActionResult(this);

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve a valuation (Maker-Checker: approver must differ from preparer).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveValuationRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin,ValuationOfficer")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectValuationRequest request, CancellationToken ct) =>
        (await _service.RejectAsync(id, request, ct)).ToActionResult(this);
}
