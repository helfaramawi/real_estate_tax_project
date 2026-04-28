using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Integrations;

namespace RealEstateTax.Application.Services;

public interface IIntegrationService
{
    Task<Result<IntegrationReceiveResultDto>> ReceiveAsync(string entityCode, ReceiveIntegrationDataRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<IntegrationRequestDto>>> GetRequestsAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<bool>> RetryAsync(Guid requestId, CancellationToken ct = default);
}
