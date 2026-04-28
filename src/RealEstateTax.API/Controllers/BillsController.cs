using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Bills;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
[Produces("application/json")]
public class BillsController : ControllerBase
{
    private readonly IBillService _service;
    public BillsController(IBillService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Generate([FromBody] GenerateBillRequest request, CancellationToken ct) =>
        (await _service.GenerateAsync(request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Issue(Guid id, [FromBody] IssueBillRequest request, CancellationToken ct) =>
        (await _service.IssueAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBillRequest request, CancellationToken ct) =>
        (await _service.CancelAsync(id, request, ct)).ToActionResult(this);
}
