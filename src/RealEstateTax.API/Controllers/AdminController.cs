using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,SuperAdmin,Auditor")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService service) => _service = service;

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAuditLogsAsync(query, ct)).ToActionResult(this);

    [HttpGet("dashboard/kpis")]
    [Authorize(Roles = "Admin,SuperAdmin,Auditor,TaxOfficer")]
    public async Task<IActionResult> GetDashboardKpis(CancellationToken ct) =>
        (await _service.GetDashboardKpisAsync(ct)).ToActionResult(this);
}
