using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Properties;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Property registration, verification and spatial search.</summary>
[ApiController]
[Route("api/properties")]
[Authorize]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _service;
    public PropertiesController(IPropertyService service) => _service = service;

    /// <summary>List all properties with optional search and pagination.</summary>
    /// <param name="query">Search term, page, and page-size parameters.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    /// <summary>Get a single property by its unique identifier.</summary>
    /// <param name="id">Property GUID.</param>
    [HttpGet("{id:guid}", Name = "GetPropertyById")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    /// <summary>Register a new property in the system.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.ToCreatedResult(this, "GetPropertyById", new { id = result.Data?.Id });
    }

    /// <summary>Update an existing property's attributes.</summary>
    /// <param name="id">Property GUID.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct) =>
        (await _service.UpdateAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Verify a property (Maker-Checker: approver must differ from creator).</summary>
    /// <param name="id">Property GUID.</param>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyPropertyRequest request, CancellationToken ct) =>
        (await _service.VerifyAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Link a taxpayer as the current owner of a property.</summary>
    /// <param name="id">Property GUID.</param>
    [HttpPost("{id:guid}/link-owner")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> LinkOwner(Guid id, [FromBody] LinkOwnerRequest request, CancellationToken ct) =>
        (await _service.LinkOwnerAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Get the full chronological audit timeline for a property.</summary>
    /// <param name="id">Property GUID.</param>
    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken ct) =>
        (await _service.GetTimelineAsync(id, ct)).ToActionResult(this);

    /// <summary>Find registered properties within a given radius of a coordinate pair (PostGIS ST_DWithin).</summary>
    /// <param name="lat">Latitude in WGS-84 decimal degrees.</param>
    /// <param name="lng">Longitude in WGS-84 decimal degrees.</param>
    /// <param name="radius">Search radius in metres (default: 500).</param>
    [HttpGet("nearby")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radius = 500,
        CancellationToken ct = default) =>
        (await _service.GetNearbyAsync(lat, lng, radius, ct)).ToActionResult(this);
}
