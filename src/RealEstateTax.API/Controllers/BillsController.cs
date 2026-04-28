using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Bills;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Tax bill lifecycle — generate, issue, and cancel bills.</summary>
[ApiController]
[Route("api/bills")]
[Authorize]
[Produces("application/json")]
public class BillsController : ControllerBase
{
    private readonly IBillService _service;
    public BillsController(IBillService service) => _service = service;

    /// <summary>List all tax bills with optional search and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    /// <summary>Get a single tax bill by its unique identifier.</summary>
    /// <param name="id">Bill GUID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    /// <summary>Generate a new tax bill from an approved tax assessment.</summary>
    /// <remarks>Bill is created in Draft status; call /issue to send it to the taxpayer.</remarks>
    [HttpPost("generate")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Generate([FromBody] GenerateBillRequest request, CancellationToken ct) =>
        (await _service.GenerateAsync(request, ct)).ToActionResult(this);

    /// <summary>Issue a bill — transitions status from Draft to Issued and triggers a notification to the taxpayer.</summary>
    /// <param name="id">Bill GUID.</param>
    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Issue(Guid id, [FromBody] IssueBillRequest request, CancellationToken ct) =>
        (await _service.IssueAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Cancel a bill with a mandatory reason (irreversible).</summary>
    /// <param name="id">Bill GUID.</param>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBillRequest request, CancellationToken ct) =>
        (await _service.CancelAsync(id, request, ct)).ToActionResult(this);
}
