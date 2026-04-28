using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Admin;

namespace RealEstateTax.Application.Services;

public interface IAdminService
{
    Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<DashboardKpiDto>> GetDashboardKpisAsync(CancellationToken ct = default);
}
