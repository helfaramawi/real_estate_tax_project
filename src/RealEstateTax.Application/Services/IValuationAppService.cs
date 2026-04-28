using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Valuations;

namespace RealEstateTax.Application.Services;

public interface IValuationAppService
{
    Task<Result<ValuationDto>> CreateAsync(CreateValuationRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<ValuationDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<Result<ValuationDto>> ApproveAsync(Guid id, ApproveValuationRequest request, CancellationToken ct = default);
    Task<Result<ValuationDto>> RejectAsync(Guid id, RejectValuationRequest request, CancellationToken ct = default);
}
