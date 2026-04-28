using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateTax.API.Extensions;
using RealEstateTax.Application.DTOs.Payments;
using RealEstateTax.Application.Services;

namespace RealEstateTax.API.Controllers;

/// <summary>Payment registration and retrieval.</summary>
[ApiController]
[Route("api/payments")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    public PaymentsController(IPaymentService service) => _service = service;

    /// <summary>Register a payment against an issued tax bill.</summary>
    /// <remarks>
    /// Supported payment methods: BankTransfer, CreditCard, DebitCard, Cash, OnlineBanking, MobileWallet.
    /// The bill's PaidAmount is updated and status transitions to PartiallyPaid or Paid accordingly.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,CollectionOfficer,TaxOfficer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Register([FromBody] RegisterPaymentRequest request, CancellationToken ct) =>
        (await _service.RegisterAsync(request, ct)).ToActionResult(this);

    /// <summary>Get all payments recorded against a specific tax bill.</summary>
    /// <param name="billId">Tax bill GUID.</param>
    [HttpGet("bill/{billId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByBill(Guid billId, CancellationToken ct) =>
        (await _service.GetByBillAsync(billId, ct)).ToActionResult(this);
}
