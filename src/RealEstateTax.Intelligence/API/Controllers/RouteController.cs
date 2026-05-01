using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;

namespace RealEstateTax.Intelligence.API.Controllers;

/// <summary>Route optimization for field inspectors.</summary>
[ApiController]
[Route("api/v2/routes")]
[Authorize]
[Produces("application/json")]
public class RouteController(IRouteOptimizationService routeService) : ControllerBase
{
    /// <summary>Generate an optimized inspection route.</summary>
    [HttpPost("optimize")]
    [FeatureGate("RouteOptimization")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> OptimizeRoute([FromBody] OptimizeRouteDto dto, CancellationToken ct)
    {
        if (dto.PropertyIds.Count == 0)
            return BadRequest(new { success = false, error = "At least one property ID is required" });
        if (dto.PropertyIds.Count > 50)
            return BadRequest(new { success = false, error = "Maximum 50 properties per route" });

        var assignment = await routeService.OptimizeRouteAsync(dto, ct);
        return Ok(new { success = true, data = assignment });
    }

    /// <summary>Get route assignments for an inspector on a given date.</summary>
    [HttpGet("assignments")]
    [FeatureGate("RouteOptimization")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] Guid inspectorId,
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var assignments = await routeService.GetAssignmentsAsync(inspectorId, d, ct);
        return Ok(new { success = true, data = assignments });
    }

    /// <summary>Update route assignment status.</summary>
    [HttpPatch("assignments/{id:guid}/status")]
    [FeatureGate("RouteOptimization")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateRouteStatusRequest request,
        CancellationToken ct)
    {
        var result = await routeService.UpdateStatusAsync(id, request.Status, ct);
        if (result is null) return NotFound(new { success = false, error = "Assignment not found" });
        return Ok(new { success = true, data = result });
    }

    /// <summary>Get GPS breadcrumb track for a route assignment.</summary>
    [HttpGet("assignments/{id:guid}/track")]
    [FeatureGate("GpsCapture")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetTrack(Guid id, CancellationToken ct)
    {
        var tracks = await routeService.GetTrackAsync(id, ct);
        return Ok(new { success = true, data = tracks });
    }
}

public record UpdateRouteStatusRequest(string Status);
