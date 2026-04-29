using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Administration — audit logs and operational dashboard KPIs.</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,SuperAdmin,Auditor")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService service) => _service = service;

    /// <summary>Query the immutable audit log with optional filtering and pagination.</summary>
    /// <remarks>
    /// Audit logs are never soft-deleted and record every state-changing action with the
    /// username, IP address, entity type/id, old values, new values, and HTTP method.
    /// </remarks>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAuditLogsAsync(query, ct)).ToActionResult(this);

    /// <summary>Get high-level operational KPIs for the dashboard (property count, bill totals, pending approvals, etc.).</summary>
    [HttpGet("dashboard/kpis")]
    [Authorize(Roles = "Admin,SuperAdmin,Auditor,TaxOfficer")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetDashboardKpis(CancellationToken ct) =>
        (await _service.GetDashboardKpisAsync(ct)).ToActionResult(this);
}
