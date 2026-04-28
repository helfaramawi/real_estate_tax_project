using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Properties;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _service;
    public PropertiesController(IPropertyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    [HttpGet("{id:guid}", Name = "GetPropertyById")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.ToCreatedResult(this, "GetPropertyById", new { id = result.Data?.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct) =>
        (await _service.UpdateAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Verify a property (Maker-Checker: approver must differ from creator).</summary>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyPropertyRequest request, CancellationToken ct) =>
        (await _service.VerifyAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Link a taxpayer as owner of a property.</summary>
    [HttpPost("{id:guid}/link-owner")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> LinkOwner(Guid id, [FromBody] LinkOwnerRequest request, CancellationToken ct) =>
        (await _service.LinkOwnerAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Get the full audit timeline for a property.</summary>
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken ct) =>
        (await _service.GetTimelineAsync(id, ct)).ToActionResult(this);

    /// <summary>Find properties within a radius (meters) of a coordinate.</summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 500, CancellationToken ct = default) =>
        (await _service.GetNearbyAsync(lat, lng, radius, ct)).ToActionResult(this);
}
