using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Integrations;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>External data integration — receive source records from government entities and monitor integration health.</summary>
[ApiController]
[Route("api/integrations")]
[Authorize]
[Produces("application/json")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _service;
    public IntegrationsController(IIntegrationService service) => _service = service;

    /// <summary>Ingest a batch of property records from a registered external government entity.</summary>
    /// <param name="entityCode">Short code of the external entity (e.g. "NUCA", "EGA", "EWEA").</param>
    /// <remarks>
    /// The payload is persisted as PropertySourceRecords and a Hangfire background job is enqueued
    /// to run 4-factor property matching (NationalId + GIS + Address + MeterNumber).
    /// </remarks>
    [HttpPost("{entityCode}/receive")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Receive(string entityCode, [FromBody] ReceiveIntegrationDataRequest request, CancellationToken ct) =>
        (await _service.ReceiveAsync(entityCode, request, ct)).ToActionResult(this);

    /// <summary>List all integration requests with optional search and pagination.</summary>
    [HttpGet("requests")]
    [Authorize(Roles = "Admin,SuperAdmin,Auditor,TaxOfficer")]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetRequests([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetRequestsAsync(query, ct)).ToActionResult(this);

    /// <summary>Retry a failed integration request (schedules a Hangfire job with a 30-second delay).</summary>
    /// <param name="id">Integration request GUID.</param>
    [HttpPost("requests/{id:guid}/retry")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct) =>
        (await _service.RetryAsync(id, ct)).ToActionResult(this);
}
