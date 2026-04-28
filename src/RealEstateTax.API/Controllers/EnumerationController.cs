using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Enumeration;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/enumeration")]
[Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
[Produces("application/json")]
public class EnumerationController : ControllerBase
{
    private readonly IEnumerationService _service;
    public EnumerationController(IEnumerationService service) => _service = service;

    [HttpPost("import-source-records")]
    public async Task<IActionResult> ImportSourceRecords([FromBody] ImportSourceRecordsRequest request, CancellationToken ct) =>
        (await _service.ImportSourceRecordsAsync(request, ct)).ToActionResult(this);

    [HttpPost("match")]
    public async Task<IActionResult> Match([FromBody] MatchSourceRecordsRequest request, CancellationToken ct) =>
        (await _service.MatchSourceRecordsAsync(request, ct)).ToActionResult(this);

    [HttpPost("create-master-record")]
    public async Task<IActionResult> CreateMasterRecord([FromBody] CreateMasterRecordRequest request, CancellationToken ct) =>
        (await _service.CreateMasterRecordAsync(request, ct)).ToActionResult(this);

    [HttpGet("unmatched-records")]
    public async Task<IActionResult> GetUnmatchedRecords([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetUnmatchedRecordsAsync(query, ct)).ToActionResult(this);

    [HttpGet("data-quality-issues")]
    public async Task<IActionResult> GetDataQualityIssues([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetDataQualityIssuesAsync(query, ct)).ToActionResult(this);
}
