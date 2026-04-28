using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Taxpayers;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/taxpayers")]
[Authorize]
[Produces("application/json")]
public class TaxpayersController : ControllerBase
{
    private readonly ITaxpayerService _service;
    public TaxpayersController(ITaxpayerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct) =>
        (await _service.GetAllAsync(query, ct)).ToActionResult(this);

    [HttpGet("{id:guid}", Name = "GetTaxpayerById")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Create([FromBody] CreateTaxpayerRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.ToCreatedResult(this, "GetTaxpayerById", new { id = result.Data?.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxpayerRequest request, CancellationToken ct) =>
        (await _service.UpdateAsync(id, request, ct)).ToActionResult(this);
}
