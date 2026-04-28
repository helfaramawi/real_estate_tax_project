using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Taxpayers;

namespace RealEstateTax.Application.Services;

public interface ITaxpayerService
{
    Task<Result<PagedResult<TaxpayerDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<TaxpayerDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaxpayerDto>> CreateAsync(CreateTaxpayerRequest request, CancellationToken ct = default);
    Task<Result<TaxpayerDto>> UpdateAsync(Guid id, UpdateTaxpayerRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
