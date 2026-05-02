using Mapster;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Properties;
using RealEstateTax.Application.Services;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Infrastructure.Services.ApplicationServices;

public class PropertyAppService : IPropertyService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public PropertyAppService(IApplicationDbContext db, IAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<PropertyDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default)
    {
        IQueryable<Property> q = _db.Properties.AsNoTracking()
            .Include(p => p.Location)
            .Include(p => p.Ownerships.Where(o => o.IsCurrent && !o.IsDeleted))
                .ThenInclude(o => o.Taxpayer);

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(p =>
                p.PropertyCode.Contains(term) ||
                (p.StreetAddress != null && p.StreetAddress.ToLower().Contains(term)) ||
                (p.Governorate != null && p.Governorate.ToLower().Contains(term)) ||
                (p.ParcelNumber != null && p.ParcelNumber.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Result<PagedResult<PropertyDto>>.Success(new PagedResult<PropertyDto>
        {
            Items = items.Adapt<List<PropertyDto>>(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<PropertyDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var property = await _db.Properties.AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Location)
            .Include(p => p.Units)
            .Include(p => p.Ownerships)
                .ThenInclude(o => o.Taxpayer)
            .Include(p => p.Valuations)
            .Include(p => p.TaxAssessments)
            .Include(p => p.TaxBills)
            .Include(p => p.RiskScores)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property == null) return Result<PropertyDetailDto>.NotFound();

        var dto = property.Adapt<PropertyDetailDto>();
        dto.LatestValuation = property.Valuations
            .OrderByDescending(v => v.TaxYear)
            .Select(v => v.Adapt<ValuationSummaryDto>())
            .FirstOrDefault();
        dto.LatestAssessment = property.TaxAssessments
            .OrderByDescending(a => a.TaxYear)
            .Select(a => a.Adapt<TaxAssessmentSummaryDto>())
            .FirstOrDefault();
        dto.LatestBill = property.TaxBills
            .OrderByDescending(b => b.IssueDate)
            .Select(b => b.Adapt<TaxBillSummaryDto>())
            .FirstOrDefault();
        dto.RiskScore = property.RiskScores
            .OrderByDescending(r => r.CalculatedAt)
            .Select(r => r.Adapt<RiskScoreSummaryDto>())
            .FirstOrDefault();

        return Result<PropertyDetailDto>.Success(dto);
    }

    public async Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken ct = default)
    {
        var property = request.Adapt<Property>();
        property.PropertyCode = await GeneratePropertyCodeAsync(ct);
        property.Status = PropertyStatus.Draft;
        property.FullAddress = BuildFullAddress(request);
        property.CreatedBy = _currentUser.Username ?? "System";

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            property.Location = new PropertyLocation
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Coordinates = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 },
                GeocodingSource = "ManualEntry",
                CreatedBy = _currentUser.Username ?? "System"
            };
        }

        await _db.Properties.AddAsync(property, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE", "Property", property.Id, property.PropertyCode, null, new { property.PropertyCode, property.Type, property.Status }, true, null, ct);

        return Result<PropertyDto>.Success(property.Adapt<PropertyDto>());
    }

    public async Task<Result<PropertyDto>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken ct = default)
    {
        var property = await _db.Properties.Include(p => p.Location).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (property == null) return Result<PropertyDto>.NotFound();

        var old = property.Adapt<PropertyDto>();
        request.Adapt(property);
        property.FullAddress = BuildFullAddress(request);

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            if (property.Location == null)
                property.Location = new PropertyLocation { PropertyId = id, CreatedBy = _currentUser.Username ?? "System" };

            property.Location.Latitude = request.Latitude;
            property.Location.Longitude = request.Longitude;
            property.Location.Coordinates = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UPDATE", "Property", id, property.PropertyCode, old, property.Adapt<PropertyDto>(), true, null, ct);

        return Result<PropertyDto>.Success(property.Adapt<PropertyDto>());
    }

    public async Task<Result<PropertyDto>> VerifyAsync(Guid id, VerifyPropertyRequest request, CancellationToken ct = default)
    {
        var property = await _db.Properties.FindAsync([id], ct);
        if (property == null) return Result<PropertyDto>.NotFound();

        if (property.Status != PropertyStatus.Draft && property.Status != PropertyStatus.NeedsReview)
            return Result<PropertyDto>.Failure("Only Draft or NeedsReview properties can be verified.");

        var old = property.Adapt<PropertyDto>();
        property.Status = PropertyStatus.Verified;
        property.VerifiedBy = _currentUser.Username;
        property.VerifiedAt = DateTime.UtcNow;
        property.VerificationNotes = request.VerificationNotes;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("VERIFY", "Property", id, property.PropertyCode, old, property.Adapt<PropertyDto>(), true, null, ct);

        return Result<PropertyDto>.Success(property.Adapt<PropertyDto>());
    }

    public async Task<Result<PropertyOwnershipDto>> LinkOwnerAsync(Guid id, LinkOwnerRequest request, CancellationToken ct = default)
    {
        var property = await _db.Properties.FindAsync([id], ct);
        if (property == null) return Result<PropertyOwnershipDto>.NotFound("Property not found.");

        var taxpayer = await _db.Taxpayers.FindAsync([request.TaxpayerId], ct);
        if (taxpayer == null) return Result<PropertyOwnershipDto>.NotFound("Taxpayer not found.");

        // End existing current ownerships if sole ownership
        if (request.OwnershipType == OwnershipType.Sole)
        {
            var existing = await _db.PropertyOwnerships
                .Where(o => o.PropertyId == id && o.IsCurrent && !o.IsDeleted).ToListAsync(ct);
            foreach (var o in existing)
            {
                o.IsCurrent = false;
                o.OwnershipEndDate = request.OwnershipStartDate.AddDays(-1);
            }
        }

        var ownership = request.Adapt<PropertyOwnership>();
        ownership.PropertyId = id;
        ownership.IsCurrent = true;
        ownership.CreatedBy = _currentUser.Username ?? "System";

        await _db.PropertyOwnerships.AddAsync(ownership, ct);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("LINK_OWNER", "Property", id, property.PropertyCode,
            null, new { TaxpayerId = request.TaxpayerId, request.OwnershipPercentage }, true, null, ct);

        var dto = ownership.Adapt<PropertyOwnershipDto>();
        dto.TaxpayerName = taxpayer.FullName;
        dto.TaxpayerNationalId = taxpayer.NationalId;
        return Result<PropertyOwnershipDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<PropertyTimelineEventDto>>> GetTimelineAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _db.Properties.AnyAsync(p => p.Id == id, ct))
            return Result<IEnumerable<PropertyTimelineEventDto>>.NotFound();

        var logs = await _db.AuditLogs
            .AsNoTracking()
            .Where(l => l.EntityId == id || (l.EntityType == "Property" && l.EntityId == id))
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync(ct);

        var timeline = logs.Select(l => new PropertyTimelineEventDto
        {
            Timestamp = l.Timestamp,
            EventType = l.Action,
            Description = l.ChangeSummary ?? $"{l.Action} on {l.EntityType}",
            PerformedBy = l.Username,
            EntityType = l.EntityType,
            EntityId = l.EntityId
        });

        return Result<IEnumerable<PropertyTimelineEventDto>>.Success(timeline);
    }

    public async Task<Result<IEnumerable<NearbyPropertyDto>>> GetNearbyAsync(double lat, double lng, double radiusMeters, CancellationToken ct = default)
    {
        // TODO: Replace with PostGIS ST_DWithin for production — this is a fallback approximate query
        var degreeTolerance = radiusMeters / 111000.0;

        var nearby = await _db.Properties.AsNoTracking()
            .Include(p => p.Location)
            .Where(p => p.Location != null
                && p.Location.Latitude.HasValue && p.Location.Longitude.HasValue
                && Math.Abs(p.Location.Latitude!.Value - lat) < degreeTolerance
                && Math.Abs(p.Location.Longitude!.Value - lng) < degreeTolerance)
            .Take(50)
            .ToListAsync(ct);

        var dtos = nearby.Select(p =>
        {
            var dLat = (p.Location!.Latitude!.Value - lat) * 111000;
            var dLng = (p.Location.Longitude!.Value - lng) * 111000 * Math.Cos(lat * Math.PI / 180);
            return new NearbyPropertyDto
            {
                Id = p.Id,
                PropertyCode = p.PropertyCode,
                Type = p.Type,
                Status = p.Status,
                Latitude = p.Location.Latitude.Value,
                Longitude = p.Location.Longitude.Value,
                DistanceMeters = Math.Round(Math.Sqrt(dLat * dLat + dLng * dLng), 1),
                Address = p.FullAddress ?? p.StreetAddress
            };
        }).OrderBy(d => d.DistanceMeters);

        return Result<IEnumerable<NearbyPropertyDto>>.Success(dtos);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (property is null) return Result<bool>.NotFound();

        property.DeletedBy = _currentUser.Username ?? "system";
        property.DeletedAt = DateTime.UtcNow;
        property.UpdatedAt = DateTime.UtcNow;
        property.UpdatedBy = _currentUser.Username ?? "system";

        _db.Properties.Remove(property); // triggers soft-delete interceptor in DbContext
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Delete", "Property", id, property.PropertyCode,
            null, new { reason }, ct: ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<BulkImportResult>> BulkImportAsync(List<CreatePropertyRequest> requests, CancellationToken ct = default)
    {
        var result = new BulkImportResult();
        for (int i = 0; i < requests.Count; i++)
        {
            var r = await CreateAsync(requests[i], ct);
            if (r.IsSuccess)
                result.Succeeded++;
            else
            {
                result.Failed++;
                result.Errors.Add(new BulkImportRowError { Row = i + 1, Message = r.Error ?? "خطأ غير معروف" });
            }
        }
        return Result<BulkImportResult>.Success(result);
    }

    private async Task<string> GeneratePropertyCodeAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Properties.CountAsync(ct) + 1;
        return $"PROP-{year}-{count:D7}";
    }

    private static string? BuildFullAddress(CreatePropertyRequest r) =>
        string.Join(", ", new[] { r.BuildingNumber, r.StreetAddress, r.Neighbourhood, r.District, r.City, r.Governorate }
            .Where(s => !string.IsNullOrEmpty(s)));
}
