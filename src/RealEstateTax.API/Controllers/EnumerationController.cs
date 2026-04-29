using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Enumeration;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Property enumeration — import source records, run 4-factor matching, and create master property records.</summary>
[ApiController]
[Route("api/enumeration")]
[Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
[Produces("application/json")]
public class EnumerationController : ControllerBase
{
    private readonly IEnumerationService _service;
    public EnumerationController(IEnumerationService service) => _service = service;

    /// <summary>Bulk-import property source records from an external system batch file.</summary>
    /// <remarks>Records are stored as PropertySourceRecords pending the matching step.</remarks>
    [HttpPost("import-source-records")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ImportSourceRecords([FromBody] ImportSourceRecordsRequest request, CancellationToken ct) =>
        (await _service.ImportSourceRecordsAsync(request, ct)).ToActionResult(this);

    /// <summary>Run the 4-factor property matching algorithm (NationalId + GIS + Address + MeterNumber) on a batch of source records.</summary>
    /// <remarks>
    /// Each factor carries a confidence weight: NationalId=0.25, GisProximity=0.35,
    /// AddressSimilarity=0.25, MeterNumber=0.15. Records scoring ≥0.60 are auto-matched.
    /// Below-threshold records appear in /unmatched-records for manual review.
    /// </remarks>
    [HttpPost("match")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Match([FromBody] MatchSourceRecordsRequest request, CancellationToken ct) =>
        (await _service.MatchSourceRecordsAsync(request, ct)).ToActionResult(this);

    /// <summary>Manually create a new master property record from one or more unmatched source records.</summary>
    [HttpPost("create-master-record")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateMasterRecord([FromBody] CreateMasterRecordRequest request, CancellationToken ct) =>
        (await _service.CreateMasterRecordAsync(request, ct)).ToActionResult(this);

    /// <summary>List source records that could not be automatically matched and require manual resolution.</summary>
    [HttpGet("unmatched-records")]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetUnmatchedRecords([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetUnmatchedRecordsAsync(query, ct)).ToActionResult(this);

    /// <summary>List open data quality issues detected during import or matching.</summary>
    [HttpGet("data-quality-issues")]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetDataQualityIssues([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetDataQualityIssuesAsync(query, ct)).ToActionResult(this);
}
