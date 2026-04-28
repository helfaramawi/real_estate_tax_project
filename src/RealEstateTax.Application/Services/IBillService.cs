using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Bills;

namespace RealEstateTax.Application.Services;

public interface IBillService
{
    Task<Result<PagedResult<TaxBillDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<TaxBillDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaxBillDto>> GenerateAsync(GenerateBillRequest request, CancellationToken ct = default);
    Task<Result<TaxBillDto>> IssueAsync(Guid id, IssueBillRequest request, CancellationToken ct = default);
    Task<Result<TaxBillDto>> CancelAsync(Guid id, CancelBillRequest request, CancellationToken ct = default);
}
