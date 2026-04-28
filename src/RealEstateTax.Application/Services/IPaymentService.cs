using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Payments;

namespace RealEstateTax.Application.Services;

public interface IPaymentService
{
    Task<Result<PaymentDto>> RegisterAsync(RegisterPaymentRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<PaymentDto>>> GetByBillAsync(Guid billId, CancellationToken ct = default);
}
