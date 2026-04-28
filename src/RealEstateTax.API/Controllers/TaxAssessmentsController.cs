using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.TaxAssessments;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/tax-assessments")]
[Authorize]
[Produces("application/json")]
public class TaxAssessmentsController : ControllerBase
{
    private readonly ITaxAssessmentAppService _service;
    public TaxAssessmentsController(ITaxAssessmentAppService service) => _service = service;

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Generate([FromBody] GenerateTaxAssessmentRequest request, CancellationToken ct) =>
        (await _service.GenerateAsync(request, ct)).ToActionResult(this);

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve tax assessment (Maker-Checker required).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveTaxAssessmentRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);
}
