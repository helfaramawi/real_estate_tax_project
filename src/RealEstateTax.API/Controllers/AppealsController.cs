using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Appeals;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/appeals")]
[Authorize]
[Produces("application/json")]
public class AppealsController : ControllerBase
{
    private readonly IAppealService _service;
    public AppealsController(IAppealService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitAppealRequest request, CancellationToken ct) =>
        (await _service.SubmitAsync(request, ct)).ToActionResult(this);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin,SuperAdmin,AppealsOfficer")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignAppealRequest request, CancellationToken ct) =>
        (await _service.AssignAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Record final decision on an appeal (Maker-Checker required).</summary>
    [HttpPost("{id:guid}/decision")]
    [Authorize(Roles = "Admin,SuperAdmin,AppealsOfficer")]
    public async Task<IActionResult> Decision(Guid id, [FromBody] AppealDecisionRequest request, CancellationToken ct) =>
        (await _service.RecordDecisionAsync(id, request, ct)).ToActionResult(this);
}
