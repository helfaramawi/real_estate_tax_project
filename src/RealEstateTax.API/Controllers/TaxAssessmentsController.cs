using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.TaxAssessments;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Tax assessments — generate from approved valuations and approve under Maker-Checker workflow.</summary>
[ApiController]
[Route("api/tax-assessments")]
[Authorize]
[Produces("application/json")]
public class TaxAssessmentsController : ControllerBase
{
    private readonly ITaxAssessmentAppService _service;
    public TaxAssessmentsController(ITaxAssessmentAppService service) => _service = service;

    /// <summary>Generate a tax assessment from an approved valuation.</summary>
    /// <remarks>
    /// Applies the applicable TaxRule (rate, brackets) and any approved Exemptions.
    /// The assessment is created in Draft status pending Maker-Checker approval.
    /// </remarks>
    [HttpPost("generate")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Generate([FromBody] GenerateTaxAssessmentRequest request, CancellationToken ct) =>
        (await _service.GenerateAsync(request, ct)).ToActionResult(this);

    /// <summary>Get all tax assessments for a specific property.</summary>
    /// <param name="propertyId">Property GUID.</param>
    [HttpGet("property/{propertyId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct) =>
        (await _service.GetByPropertyAsync(propertyId, ct)).ToActionResult(this);

    /// <summary>Approve a tax assessment (Maker-Checker: approver must differ from the preparer).</summary>
    /// <param name="id">Tax assessment GUID.</param>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveTaxAssessmentRequest request, CancellationToken ct) =>
        (await _service.ApproveAsync(id, request, ct)).ToActionResult(this);
}
