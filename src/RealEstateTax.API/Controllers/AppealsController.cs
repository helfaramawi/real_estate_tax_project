using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Appeals;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Tax assessment appeals — submit, assign, and decide.</summary>
[ApiController]
[Route("api/appeals")]
[Authorize]
[Produces("application/json")]
public class AppealsController : ControllerBase
{
    private readonly IAppealService _service;
    public AppealsController(IAppealService service) => _service = service;

    /// <summary>Submit a new appeal against a tax assessment or bill.</summary>
    /// <remarks>
    /// Any authenticated user (taxpayer or officer) may submit an appeal.
    /// The grounds summary is mandatory; the detailed legal statement and supporting
    /// documents should be attached via the document upload endpoint.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Submit([FromBody] SubmitAppealRequest request, CancellationToken ct) =>
        (await _service.SubmitAsync(request, ct)).ToActionResult(this);

    /// <summary>List all appeals with optional search and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    /// <summary>Get a single appeal by its unique identifier.</summary>
    /// <param name="id">Appeal GUID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    /// <summary>Assign an appeal to an appeals officer for investigation.</summary>
    /// <param name="id">Appeal GUID.</param>
    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin,SuperAdmin,AppealsOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignAppealRequest request, CancellationToken ct) =>
        (await _service.AssignAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Record the final decision on an appeal (Maker-Checker required — officer cannot decide own appeal).</summary>
    /// <param name="id">Appeal GUID.</param>
    [HttpPost("{id:guid}/decision")]
    [Authorize(Roles = "Admin,SuperAdmin,AppealsOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Decision(Guid id, [FromBody] AppealDecisionRequest request, CancellationToken ct) =>
        (await _service.RecordDecisionAsync(id, request, ct)).ToActionResult(this);
}
