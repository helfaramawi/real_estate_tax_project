using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Exemptions;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/exemptions")]
[Authorize]
[Produces("application/json")]
public class ExemptionsController : ControllerBase
{
    private readonly IExemptionAppService _service;
    public ExemptionsController(IExemptionAppService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,Citizen")]
    public async Task<IActionResult> Submit([FromBody] SubmitExemptionRequest request, CancellationToken ct) =>
        (await _service.SubmitAsync(request, ct)).ToActionResult(this);

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve exemption (Maker-Checker: reviewer cannot approve own review).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveExemptionRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExemptionRequest request, CancellationToken ct) =>
        (await _service.RejectAsync(id, request, ct)).ToActionResult(this);
}
