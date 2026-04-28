using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Integrations;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
[Produces("application/json")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _service;
    public IntegrationsController(IIntegrationService service) => _service = service;

    /// <summary>Receive data from an external government entity.</summary>
    [HttpPost("{entityCode}/receive")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Receive(string entityCode, [FromBody] ReceiveIntegrationDataRequest request, CancellationToken ct) =>
        (await _service.ReceiveAsync(entityCode, request, ct)).ToActionResult(this);

    [HttpGet("requests")]
    [Authorize(Roles = "Admin,SuperAdmin,Auditor,TaxOfficer")]
    public async Task<IActionResult> GetRequests([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetRequestsAsync(query, ct)).ToActionResult(this);

    [HttpPost("requests/{id:guid}/retry")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct) =>
        (await _service.RetryAsync(id, ct)).ToActionResult(this);
}
