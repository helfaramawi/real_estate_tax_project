using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Properties;

namespace RealEstateTax.Application.Services;

public interface IPropertyService
{
    Task<Result<PagedResult<PropertyDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<PropertyDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken ct = default);
    Task<Result<PropertyDto>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken ct = default);
    Task<Result<PropertyDto>> VerifyAsync(Guid id, VerifyPropertyRequest request, CancellationToken ct = default);
    Task<Result<PropertyOwnershipDto>> LinkOwnerAsync(Guid id, LinkOwnerRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<PropertyTimelineEventDto>>> GetTimelineAsync(Guid id, CancellationToken ct = default);
    Task<Result<IEnumerable<NearbyPropertyDto>>> GetNearbyAsync(double lat, double lng, double radiusMeters, CancellationToken ct = default);
}
