using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Appeals;

namespace RealEstateTax.Application.Services;

public interface IAppealService
{
    Task<Result<AppealDto>> SubmitAsync(SubmitAppealRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<AppealDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<AppealDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<AppealDto>> AssignAsync(Guid id, AssignAppealRequest request, CancellationToken ct = default);
    Task<Result<AppealDto>> RecordDecisionAsync(Guid id, AppealDecisionRequest request, CancellationToken ct = default);
}
