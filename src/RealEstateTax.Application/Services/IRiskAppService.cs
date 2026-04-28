using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Risk;

namespace RealEstateTax.Application.Services;

public interface IRiskAppService
{
    Task<Result<RiskScoreDto>> GetRiskScoreAsync(Guid propertyId, CancellationToken ct = default);
    Task<Result<RiskScoreDto>> RecalculateAsync(Guid propertyId, CancellationToken ct = default);
    Task<Result<PagedResult<FraudFlagDto>>> GetFraudFlagsAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<FraudFlagDto>> CreateFraudFlagAsync(CreateFraudFlagRequest request, CancellationToken ct = default);
}
