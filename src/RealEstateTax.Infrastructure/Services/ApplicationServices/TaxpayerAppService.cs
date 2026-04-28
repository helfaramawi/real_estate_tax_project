using Mapster;
using Microsoft.EntityFrameworkCore;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Taxpayers;
using RealEstateTax.Application.Services;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Services.ApplicationServices;

public class TaxpayerAppService : ITaxpayerService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public TaxpayerAppService(IApplicationDbContext db, IAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<TaxpayerDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default)
    {
        IQueryable<Taxpayer> q = _db.Taxpayers.AsNoTracking()
            .Include(t => t.PropertyOwnerships.Where(o => o.IsCurrent && !o.IsDeleted));

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(t =>
                t.NationalId.Contains(term) ||
                t.FirstName.ToLower().Contains(term) ||
                t.LastName.ToLower().Contains(term) ||
                (t.CompanyName != null && t.CompanyName.ToLower().Contains(term)) ||
                t.TaxpayerCode.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Result<PagedResult<TaxpayerDto>>.Success(new PagedResult<TaxpayerDto>
        {
            Items = items.Adapt<List<TaxpayerDto>>(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<TaxpayerDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var taxpayer = await _db.Taxpayers
            .AsNoTracking()
            .Include(t => t.PropertyOwnerships.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Property)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (taxpayer == null) return Result<TaxpayerDetailDto>.NotFound();

        return Result<TaxpayerDetailDto>.Success(taxpayer.Adapt<TaxpayerDetailDto>());
    }

    public async Task<Result<TaxpayerDto>> CreateAsync(CreateTaxpayerRequest request, CancellationToken ct = default)
    {
        var existing = await _db.Taxpayers
            .FirstOrDefaultAsync(t => t.NationalId == request.NationalId, ct);
        if (existing != null)
            return Result<TaxpayerDto>.Failure($"A taxpayer with National ID {request.NationalId} already exists.", "DUPLICATE");

        var taxpayer = request.Adapt<Taxpayer>();
        taxpayer.TaxpayerCode = await GenerateTaxpayerCodeAsync(ct);
        taxpayer.CreatedBy = _currentUser.Username ?? "System";

        await _db.Taxpayers.AddAsync(taxpayer, ct);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("CREATE", "Taxpayer", taxpayer.Id, taxpayer.TaxpayerCode, null, taxpayer, true, null, ct);

        return Result<TaxpayerDto>.Success(taxpayer.Adapt<TaxpayerDto>());
    }

    public async Task<Result<TaxpayerDto>> UpdateAsync(Guid id, UpdateTaxpayerRequest request, CancellationToken ct = default)
    {
        var taxpayer = await _db.Taxpayers.FindAsync([id], ct);
        if (taxpayer == null) return Result<TaxpayerDto>.NotFound();

        var old = taxpayer.Adapt<TaxpayerDto>();
        request.Adapt(taxpayer);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("UPDATE", "Taxpayer", id, taxpayer.TaxpayerCode, old, taxpayer.Adapt<TaxpayerDto>(), true, null, ct);

        return Result<TaxpayerDto>.Success(taxpayer.Adapt<TaxpayerDto>());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var taxpayer = await _db.Taxpayers.FindAsync([id], ct);
        if (taxpayer == null) return Result<bool>.NotFound();

        taxpayer.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DELETE", "Taxpayer", id, taxpayer.TaxpayerCode, null, null, true, null, ct);

        return Result<bool>.Success(true);
    }

    private async Task<string> GenerateTaxpayerCodeAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Taxpayers.CountAsync(ct) + 1;
        return $"TP-{year}-{count:D6}";
    }
}
