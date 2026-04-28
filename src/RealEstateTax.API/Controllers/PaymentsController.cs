using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Payments;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    public PaymentsController(IPaymentService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,CollectionOfficer,TaxOfficer")]
    public async Task<IActionResult> Register([FromBody] RegisterPaymentRequest request, CancellationToken ct) =>
        (await _service.RegisterAsync(request, ct)).ToActionResult(this);

    [HttpGet("bill/{billId:guid}")]
    public async Task<IActionResult> GetByBill(Guid billId, CancellationToken ct) =>
        (await _service.GetByBillAsync(billId, ct)).ToActionResult(this);
}
