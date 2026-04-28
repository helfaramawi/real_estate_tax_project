using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Taxpayers;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Taxpayer registration and management.</summary>
[ApiController]
[Route("api/taxpayers")]
[Authorize]
[Produces("application/json")]
public class TaxpayersController : ControllerBase
{
    private readonly ITaxpayerService _service;
    public TaxpayersController(ITaxpayerService service) => _service = service;

    /// <summary>List all taxpayers with optional search and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<object>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    /// <summary>Get a single taxpayer by their unique identifier.</summary>
    /// <param name="id">Taxpayer GUID.</param>
    [HttpGet("{id:guid}", Name = "GetTaxpayerById")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    /// <summary>Register a new taxpayer. The Egyptian National ID (14 digits) is structurally validated.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateTaxpayerRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.ToCreatedResult(this, "GetTaxpayerById", new { id = result.Data?.Id });
    }

    /// <summary>Update an existing taxpayer's information.</summary>
    /// <param name="id">Taxpayer GUID.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxpayerRequest request, CancellationToken ct) =>
        (await _service.UpdateAsync(id, request, ct)).ToActionResult(this);
}
