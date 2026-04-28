using Mapster;
using Microsoft.EntityFrameworkCore;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Admin;
using RealEstateTax.Application.DTOs.Appeals;
using RealEstateTax.Application.DTOs.Bills;
using RealEstateTax.Application.DTOs.Enumeration;
using RealEstateTax.Application.DTOs.Exemptions;
using RealEstateTax.Application.DTOs.Integrations;
using RealEstateTax.Application.DTOs.Payments;
using RealEstateTax.Application.DTOs.Risk;
using RealEstateTax.Application.DTOs.TaxAssessments;
using RealEstateTax.Application.DTOs.Valuations;
using RealEstateTax.Application.Services;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services.ApplicationServices;

// ────────────────────────────────────────────────────────────────────
// Enumeration
// ────────────────────────────────────────────────────────────────────
public class EnumerationAppService : IEnumerationService
{
    private readonly IApplicationDbContext _db;
    private readonly IPropertyMatchingService _matcher;
    private readonly ICurrentUserService _user;
    private readonly IAuditService _audit;

    public EnumerationAppService(IApplicationDbContext db, IPropertyMatchingService matcher, ICurrentUserService user, IAuditService audit)
    { _db = db; _matcher = matcher; _user = user; _audit = audit; }

    public async Task<Result<ImportResultDto>> ImportSourceRecordsAsync(ImportSourceRecordsRequest request, CancellationToken ct = default)
    {
        var source = await _db.PropertySources.FindAsync([request.SourceId], ct);
        if (source == null) return Result<ImportResultDto>.NotFound("PropertySource not found.");

        int imported = 0, existed = 0, failed = 0;
        var errors = new List<string>();
        var batchId = request.BatchId ?? $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

        foreach (var item in request.Records)
        {
            try
            {
                var exists = await _db.PropertySourceRecords
                    .AnyAsync(r => r.SourceId == request.SourceId && r.SourceReferenceId == item.SourceReferenceId, ct);
                if (exists) { existed++; continue; }

                var record = item.Adapt<PropertySourceRecord>();
                record.SourceId = request.SourceId;
                record.ImportBatchId = batchId;
                record.ImportedAt = DateTime.UtcNow;
                record.CreatedBy = _user.Username ?? "System";
                await _db.PropertySourceRecords.AddAsync(record, ct);
                imported++;
            }
            catch (Exception ex) { failed++; errors.Add($"{item.SourceReferenceId}: {ex.Message}"); }
        }

        source.TotalRecordsImported += imported;
        source.LastSyncAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<ImportResultDto>.Success(new ImportResultDto { TotalImported = imported, AlreadyExisted = existed, Failed = failed, BatchId = batchId, Errors = errors });
    }

    public async Task<Result<IEnumerable<MatchResultDto>>> MatchSourceRecordsAsync(MatchSourceRecordsRequest request, CancellationToken ct = default)
    {
        var results = new List<MatchResultDto>();
        foreach (var id in request.SourceRecordIds)
        {
            var record = await _db.PropertySourceRecords.FindAsync([id], ct);
            if (record == null) continue;

            var match = await _matcher.FindMatchAsync(record, ct);
            if (match.IsMatch && match.MatchedPropertyId.HasValue && match.ConfidenceScore >= request.MinConfidenceThreshold)
            {
                record.IsMatched = true;
                record.MasterPropertyId = match.MatchedPropertyId;
                record.MatchConfidenceScore = match.ConfidenceScore;
                record.MatchMethod = match.MatchMethod;
                record.MatchedAt = DateTime.UtcNow;
            }

            results.Add(new MatchResultDto { SourceRecordId = id, IsMatched = match.IsMatch, MasterPropertyId = match.MatchedPropertyId, ConfidenceScore = match.ConfidenceScore, MatchMethod = match.MatchMethod, ContributingFactors = match.ContributingFactors });
        }
        await _db.SaveChangesAsync(ct);
        return Result<IEnumerable<MatchResultDto>>.Success(results);
    }

    public async Task<Result<Guid>> CreateMasterRecordAsync(CreateMasterRecordRequest request, CancellationToken ct = default)
    {
        var sourceRecords = await _db.PropertySourceRecords
            .Where(r => request.SourceRecordIds.Contains(r.Id)).ToListAsync(ct);
        if (!sourceRecords.Any()) return Result<Guid>.Failure("No source records found.");

        var primary = sourceRecords.First();
        var property = new Property
        {
            PropertyCode = $"PROP-{DateTime.UtcNow.Year}-{await _db.Properties.CountAsync(ct) + 1:D7}",
            Status = PropertyStatus.NeedsReview,
            BuiltUpArea = primary.AreaFromSource ?? 0,
            FullAddress = primary.Address,
            CreatedBy = _user.Username ?? "System"
        };

        if (!string.IsNullOrEmpty(request.OverrideAddress)) property.FullAddress = request.OverrideAddress;

        await _db.Properties.AddAsync(property, ct);

        if (primary.Latitude.HasValue && primary.Longitude.HasValue)
        {
            await _db.PropertyLocations.AddAsync(new PropertyLocation
            {
                PropertyId = property.Id,
                Latitude = primary.Latitude,
                Longitude = primary.Longitude,
                CreatedBy = _user.Username ?? "System"
            }, ct);
        }

        foreach (var r in sourceRecords)
        {
            r.MasterPropertyId = property.Id;
            r.IsMatched = true;
            r.MatchedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE_MASTER", "Property", property.Id, property.PropertyCode, null, new { SourceRecordCount = sourceRecords.Count }, true, null, ct);

        return Result<Guid>.Success(property.Id);
    }

    public async Task<Result<PagedResult<UnmatchedRecordDto>>> GetUnmatchedRecordsAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.PropertySourceRecords.AsNoTracking()
            .Include(r => r.Source)
            .Where(r => !r.IsMatched && !r.IsRejectedForMatch);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(r => r.ImportedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return Result<PagedResult<UnmatchedRecordDto>>.Success(new PagedResult<UnmatchedRecordDto>
        { Items = items.Select(r => new UnmatchedRecordDto { Id = r.Id, SourceReferenceId = r.SourceReferenceId, SourceCode = r.Source.Code, OwnerName = r.OwnerName, OwnerNationalId = r.OwnerNationalId, Address = r.Address, Latitude = r.Latitude, Longitude = r.Longitude, ImportedAt = r.ImportedAt }), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<PagedResult<DataQualityIssueDto>>> GetDataQualityIssuesAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.DataQualityIssues.AsNoTracking().Include(i => i.Property);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(i => i.CreatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return Result<PagedResult<DataQualityIssueDto>>.Success(new PagedResult<DataQualityIssueDto>
        { Items = items.Select(i => new DataQualityIssueDto { Id = i.Id, PropertyId = i.PropertyId, PropertyCode = i.Property.PropertyCode, IssueType = i.IssueType, Severity = i.Severity, Status = i.Status, Description = i.Description, AffectedField = i.AffectedField, SuggestedAction = i.SuggestedAction, CreatedAt = i.CreatedAt }), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }
}

// ────────────────────────────────────────────────────────────────────
// Valuation
// ────────────────────────────────────────────────────────────────────
public class ValuationApplicationService : IValuationAppService
{
    private readonly IApplicationDbContext _db;
    private readonly IValuationService _valuationDomain;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public ValuationApplicationService(IApplicationDbContext db, IValuationService valuationDomain, IAuditService audit, ICurrentUserService user)
    { _db = db; _valuationDomain = valuationDomain; _audit = audit; _user = user; }

    public async Task<Result<ValuationDto>> CreateAsync(CreateValuationRequest request, CancellationToken ct = default)
    {
        var property = await _db.Properties.FindAsync([request.PropertyId], ct);
        if (property == null) return Result<ValuationDto>.NotFound("Property not found.");

        ValuationRule? rule = null;
        if (request.ValuationRuleId.HasValue)
            rule = await _db.ValuationRules.FindAsync([request.ValuationRuleId], ct);
        rule ??= await _db.ValuationRules.FirstOrDefaultAsync(r => r.IsActive, ct);
        if (rule == null) return Result<ValuationDto>.Failure("No active valuation rule found.");

        var result = await _valuationDomain.ComputeValueAsync(property, request.Method, rule, ct);

        var valuation = request.Adapt<Valuation>();
        valuation.ValuationCode = $"VAL-{DateTime.UtcNow.Year}-{await _db.Valuations.CountAsync(ct) + 1:D6}";
        valuation.GrossValue = result.GrossValue;
        valuation.DeductionPercentage = result.DeductionPercentage;
        valuation.NetValue = result.NetValue;
        valuation.Status = ValuationStatus.Draft;
        valuation.PreparedBy = _user.Username;
        valuation.PreparedAt = DateTime.UtcNow;
        valuation.ValuationRuleId = rule.Id;
        valuation.CreatedBy = _user.Username ?? "System";

        await _db.Valuations.AddAsync(valuation, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE", "Valuation", valuation.Id, valuation.ValuationCode, null, new { valuation.NetValue, valuation.TaxYear }, true, null, ct);

        return Result<ValuationDto>.Success(valuation.Adapt<ValuationDto>());
    }

    public async Task<Result<IEnumerable<ValuationDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        var items = await _db.Valuations.AsNoTracking().Include(v => v.Property).Where(v => v.PropertyId == propertyId).OrderByDescending(v => v.TaxYear).ToListAsync(ct);
        return Result<IEnumerable<ValuationDto>>.Success(items.Adapt<List<ValuationDto>>());
    }

    public async Task<Result<ValuationDto>> ApproveAsync(Guid id, ApproveValuationRequest request, CancellationToken ct = default)
    {
        var val = await _db.Valuations.Include(v => v.Property).FirstOrDefaultAsync(v => v.Id == id, ct);
        if (val == null) return Result<ValuationDto>.NotFound();
        if (val.PreparedBy == _user.Username) return Result<ValuationDto>.Forbidden("Maker cannot be checker. Approval requires a different officer.");

        val.Status = ValuationStatus.Approved;
        val.ApprovedBy = _user.Username;
        val.ApprovedAt = DateTime.UtcNow;
        val.ApprovalNotes = request.ApprovalNotes;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("APPROVE", "Valuation", id, val.ValuationCode, null, new { val.Status }, true, null, ct);

        return Result<ValuationDto>.Success(val.Adapt<ValuationDto>());
    }

    public async Task<Result<ValuationDto>> RejectAsync(Guid id, RejectValuationRequest request, CancellationToken ct = default)
    {
        var val = await _db.Valuations.Include(v => v.Property).FirstOrDefaultAsync(v => v.Id == id, ct);
        if (val == null) return Result<ValuationDto>.NotFound();
        val.Status = ValuationStatus.Rejected;
        val.RejectionReason = request.RejectionReason;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("REJECT", "Valuation", id, val.ValuationCode, null, new { val.Status }, true, null, ct);
        return Result<ValuationDto>.Success(val.Adapt<ValuationDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Tax Assessment
// ────────────────────────────────────────────────────────────────────
public class TaxAssessmentAppService : ITaxAssessmentAppService
{
    private readonly IApplicationDbContext _db;
    private readonly ITaxCalculationService _calc;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public TaxAssessmentAppService(IApplicationDbContext db, ITaxCalculationService calc, IAuditService audit, ICurrentUserService user)
    { _db = db; _calc = calc; _audit = audit; _user = user; }

    public async Task<Result<TaxAssessmentDto>> GenerateAsync(GenerateTaxAssessmentRequest request, CancellationToken ct = default)
    {
        var property = await _db.Properties.FindAsync([request.PropertyId], ct);
        if (property == null) return Result<TaxAssessmentDto>.NotFound("Property not found.");

        var valuation = await _db.Valuations.FirstOrDefaultAsync(v => v.Id == request.ValuationId && v.Status == ValuationStatus.Approved, ct);
        if (valuation == null) return Result<TaxAssessmentDto>.Failure("Approved valuation not found.");

        TaxRule? rule = request.TaxRuleId.HasValue
            ? await _db.TaxRules.FindAsync([request.TaxRuleId], ct)
            : await _db.TaxRules.FirstOrDefaultAsync(r => r.IsActive && r.PropertyType == property.Type.ToString(), ct)
              ?? await _db.TaxRules.FirstOrDefaultAsync(r => r.IsActive, ct);
        if (rule == null) return Result<TaxAssessmentDto>.Failure("No active tax rule found.");

        Exemption? exemption = request.ExemptionId.HasValue
            ? await _db.Exemptions.FindAsync([request.ExemptionId], ct)
            : await _db.Exemptions.FirstOrDefaultAsync(e => e.PropertyId == request.PropertyId && e.Status == ExemptionStatus.Approved && (e.EffectiveTo == null || e.EffectiveTo >= DateTime.UtcNow), ct);

        var calc = await _calc.CalculateAsync(property, valuation, rule, exemption, ct);

        var assessment = new TaxAssessment
        {
            AssessmentCode = $"ASS-{request.TaxYear}-{await _db.TaxAssessments.CountAsync(ct) + 1:D6}",
            PropertyId = request.PropertyId,
            ValuationId = request.ValuationId,
            TaxYear = request.TaxYear,
            Status = AssessmentStatus.Draft,
            TaxableValue = calc.TaxableValue,
            TaxRate = calc.TaxRate,
            GrossTaxAmount = calc.GrossTaxAmount,
            ExemptionAmount = calc.ExemptionAmount,
            NetTaxAmount = calc.NetTaxAmount,
            TotalDue = calc.NetTaxAmount,
            TaxRuleId = rule.Id,
            ExemptionId = exemption?.Id,
            PreparedBy = _user.Username,
            PreparedAt = DateTime.UtcNow,
            CreatedBy = _user.Username ?? "System"
        };

        await _db.TaxAssessments.AddAsync(assessment, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("GENERATE", "TaxAssessment", assessment.Id, assessment.AssessmentCode, null, new { assessment.NetTaxAmount, assessment.TaxYear }, true, null, ct);

        return Result<TaxAssessmentDto>.Success(assessment.Adapt<TaxAssessmentDto>());
    }

    public async Task<Result<IEnumerable<TaxAssessmentDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        var items = await _db.TaxAssessments.AsNoTracking().Include(a => a.Property).Where(a => a.PropertyId == propertyId).OrderByDescending(a => a.TaxYear).ToListAsync(ct);
        return Result<IEnumerable<TaxAssessmentDto>>.Success(items.Adapt<List<TaxAssessmentDto>>());
    }

    public async Task<Result<TaxAssessmentDto>> ApproveAsync(Guid id, ApproveTaxAssessmentRequest request, CancellationToken ct = default)
    {
        var assessment = await _db.TaxAssessments.Include(a => a.Property).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assessment == null) return Result<TaxAssessmentDto>.NotFound();
        if (assessment.PreparedBy == _user.Username) return Result<TaxAssessmentDto>.Forbidden("Maker cannot approve own assessment.");

        assessment.Status = AssessmentStatus.Approved;
        assessment.ApprovedBy = _user.Username;
        assessment.ApprovedAt = DateTime.UtcNow;
        assessment.ApprovalNotes = request.ApprovalNotes;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("APPROVE", "TaxAssessment", id, assessment.AssessmentCode, null, new { assessment.Status }, true, null, ct);

        return Result<TaxAssessmentDto>.Success(assessment.Adapt<TaxAssessmentDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Bills
// ────────────────────────────────────────────────────────────────────
public class BillAppService : IBillService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public BillAppService(IApplicationDbContext db, INotificationService notify, IAuditService audit, ICurrentUserService user)
    { _db = db; _notify = notify; _audit = audit; _user = user; }

    public async Task<Result<PagedResult<TaxBillDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.TaxBills.AsNoTracking().Include(b => b.Property).Include(b => b.Taxpayer);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(b => b.CreatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Result<PagedResult<TaxBillDto>>.Success(new PagedResult<TaxBillDto> { Items = items.Adapt<List<TaxBillDto>>(), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<TaxBillDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bill = await _db.TaxBills.AsNoTracking().Include(b => b.Property).Include(b => b.Taxpayer).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bill == null) return Result<TaxBillDto>.NotFound();
        return Result<TaxBillDto>.Success(bill.Adapt<TaxBillDto>());
    }

    public async Task<Result<TaxBillDto>> GenerateAsync(GenerateBillRequest request, CancellationToken ct = default)
    {
        var assessment = await _db.TaxAssessments.Include(a => a.Property).FirstOrDefaultAsync(a => a.Id == request.TaxAssessmentId && a.Status == AssessmentStatus.Approved, ct);
        if (assessment == null) return Result<TaxBillDto>.Failure("Approved tax assessment not found.");

        var ownership = await _db.PropertyOwnerships.Include(o => o.Taxpayer).FirstOrDefaultAsync(o => o.PropertyId == assessment.PropertyId && o.IsCurrent, ct);
        if (ownership == null) return Result<TaxBillDto>.Failure("No current property owner found to bill.");

        var bill = new TaxBill
        {
            BillNumber = $"BILL-{assessment.TaxYear}-{await _db.TaxBills.CountAsync(ct) + 1:D7}",
            PropertyId = assessment.PropertyId,
            TaxAssessmentId = assessment.Id,
            TaxpayerId = ownership.TaxpayerId,
            TaxYear = assessment.TaxYear,
            Status = BillStatus.Draft,
            TotalAmount = assessment.TotalDue,
            PaidAmount = 0,
            IssueDate = DateTime.UtcNow.Date,
            DueDate = request.DueDate,
            AllowsInstallments = request.AllowsInstallments,
            NumberOfInstallments = request.NumberOfInstallments,
            CreatedBy = _user.Username ?? "System"
        };

        await _db.TaxBills.AddAsync(bill, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("GENERATE", "TaxBill", bill.Id, bill.BillNumber, null, new { bill.TotalAmount }, true, null, ct);

        return Result<TaxBillDto>.Success(bill.Adapt<TaxBillDto>());
    }

    public async Task<Result<TaxBillDto>> IssueAsync(Guid id, IssueBillRequest request, CancellationToken ct = default)
    {
        var bill = await _db.TaxBills.Include(b => b.Property).Include(b => b.Taxpayer).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bill == null) return Result<TaxBillDto>.NotFound();
        if (bill.Status != BillStatus.Draft) return Result<TaxBillDto>.Failure("Only Draft bills can be issued.");

        bill.Status = BillStatus.Issued;
        bill.IssuedAt = DateTime.UtcNow;
        bill.IssuedBy = _user.Username;
        if (request.OverrideDueDate.HasValue) bill.DueDate = request.OverrideDueDate.Value;
        await _db.SaveChangesAsync(ct);

        await _notify.SendBillIssuedAsync(bill.TaxpayerId, bill.Id, bill.TotalAmount, ct);
        await _audit.LogAsync("ISSUE", "TaxBill", id, bill.BillNumber, null, new { bill.Status }, true, null, ct);

        return Result<TaxBillDto>.Success(bill.Adapt<TaxBillDto>());
    }

    public async Task<Result<TaxBillDto>> CancelAsync(Guid id, CancelBillRequest request, CancellationToken ct = default)
    {
        var bill = await _db.TaxBills.Include(b => b.Property).Include(b => b.Taxpayer).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bill == null) return Result<TaxBillDto>.NotFound();
        if (bill.Status == BillStatus.Paid) return Result<TaxBillDto>.Failure("Fully paid bills cannot be cancelled.");

        bill.Status = BillStatus.Cancelled;
        bill.CancelledAt = DateTime.UtcNow;
        bill.CancelledBy = _user.Username;
        bill.CancellationReason = request.CancellationReason;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CANCEL", "TaxBill", id, bill.BillNumber, null, new { bill.Status, request.CancellationReason }, true, null, ct);

        return Result<TaxBillDto>.Success(bill.Adapt<TaxBillDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Payments
// ────────────────────────────────────────────────────────────────────
public class PaymentAppService : IPaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public PaymentAppService(IApplicationDbContext db, INotificationService notify, IAuditService audit, ICurrentUserService user)
    { _db = db; _notify = notify; _audit = audit; _user = user; }

    public async Task<Result<PaymentDto>> RegisterAsync(RegisterPaymentRequest request, CancellationToken ct = default)
    {
        var bill = await _db.TaxBills.Include(b => b.Taxpayer).FirstOrDefaultAsync(b => b.Id == request.TaxBillId, ct);
        if (bill == null) return Result<PaymentDto>.NotFound("Tax bill not found.");
        if (bill.Status == BillStatus.Cancelled) return Result<PaymentDto>.Failure("Cannot accept payment on a cancelled bill.");
        if (bill.Status == BillStatus.Paid) return Result<PaymentDto>.Failure("Bill is already fully paid.");
        if (request.Amount > bill.RemainingAmount) return Result<PaymentDto>.Failure($"Payment amount {request.Amount:N2} exceeds remaining balance {bill.RemainingAmount:N2} EGP.");

        var payment = new Payment
        {
            PaymentReference = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TaxBillId = bill.Id,
            TaxpayerId = bill.TaxpayerId,
            Status = PaymentStatus.Completed,
            Method = request.Method,
            Amount = request.Amount,
            PaidAt = request.PaidAt ?? DateTime.UtcNow,
            ExternalTransactionId = request.ExternalTransactionId,
            BankReference = request.BankReference,
            Notes = request.Notes,
            CreatedBy = _user.Username ?? "System"
        };

        bill.PaidAmount += request.Amount;
        bill.Status = bill.PaidAmount >= bill.TotalAmount ? BillStatus.Paid : BillStatus.PartiallyPaid;

        await _db.Payments.AddAsync(payment, ct);
        await _db.SaveChangesAsync(ct);

        await _notify.SendPaymentConfirmationAsync(bill.TaxpayerId, payment.Id, request.Amount, ct);
        await _audit.LogAsync("REGISTER_PAYMENT", "Payment", payment.Id, payment.PaymentReference, null, new { payment.Amount, payment.Method }, true, null, ct);

        return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());
    }

    public async Task<Result<IEnumerable<PaymentDto>>> GetByBillAsync(Guid billId, CancellationToken ct = default)
    {
        var items = await _db.Payments.AsNoTracking().Include(p => p.TaxBill).Include(p => p.Taxpayer).Where(p => p.TaxBillId == billId).ToListAsync(ct);
        return Result<IEnumerable<PaymentDto>>.Success(items.Adapt<List<PaymentDto>>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Appeals
// ────────────────────────────────────────────────────────────────────
public class AppealAppService : IAppealService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public AppealAppService(IApplicationDbContext db, INotificationService notify, IAuditService audit, ICurrentUserService user)
    { _db = db; _notify = notify; _audit = audit; _user = user; }

    public async Task<Result<AppealDto>> SubmitAsync(SubmitAppealRequest request, CancellationToken ct = default)
    {
        var appeal = request.Adapt<Appeal>();
        appeal.AppealCode = $"APP-{DateTime.UtcNow.Year}-{await _db.Appeals.CountAsync(ct) + 1:D6}";
        appeal.Status = AppealStatus.Submitted;
        appeal.SubmittedAt = DateTime.UtcNow;
        // TODO: Set deadline from Egyptian law (typically 60 days from assessment notice date)
        appeal.Deadline = DateTime.UtcNow.AddDays(60);
        appeal.CreatedBy = _user.Username ?? "System";

        await _db.Appeals.AddAsync(appeal, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("SUBMIT", "Appeal", appeal.Id, appeal.AppealCode, null, new { appeal.GroundsSummary }, true, null, ct);

        return Result<AppealDto>.Success(appeal.Adapt<AppealDto>());
    }

    public async Task<Result<PagedResult<AppealDto>>> GetAllAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.Appeals.AsNoTracking().Include(a => a.Property).Include(a => a.Taxpayer).Include(a => a.AssignedToUser);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.SubmittedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Result<PagedResult<AppealDto>>.Success(new PagedResult<AppealDto> { Items = items.Adapt<List<AppealDto>>(), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<AppealDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var appeal = await _db.Appeals.AsNoTracking().Include(a => a.Property).Include(a => a.Taxpayer).Include(a => a.AssignedToUser).Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appeal == null) return Result<AppealDetailDto>.NotFound();
        return Result<AppealDetailDto>.Success(appeal.Adapt<AppealDetailDto>());
    }

    public async Task<Result<AppealDto>> AssignAsync(Guid id, AssignAppealRequest request, CancellationToken ct = default)
    {
        var appeal = await _db.Appeals.Include(a => a.Property).Include(a => a.Taxpayer).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appeal == null) return Result<AppealDto>.NotFound();

        appeal.AssignedToUserId = request.AssignedToUserId;
        appeal.AssignedAt = DateTime.UtcNow;
        appeal.HearingDate = request.HearingDate;
        appeal.Status = AppealStatus.Assigned;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ASSIGN", "Appeal", id, appeal.AppealCode, null, new { request.AssignedToUserId }, true, null, ct);

        return Result<AppealDto>.Success(appeal.Adapt<AppealDto>());
    }

    public async Task<Result<AppealDto>> RecordDecisionAsync(Guid id, AppealDecisionRequest request, CancellationToken ct = default)
    {
        var appeal = await _db.Appeals.Include(a => a.Property).Include(a => a.Taxpayer).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appeal == null) return Result<AppealDto>.NotFound();

        appeal.Status = request.Decision;
        appeal.DecisionBy = _user.Username;
        appeal.DecisionAt = DateTime.UtcNow;
        appeal.DecisionNotes = request.DecisionNotes;
        appeal.RevisedAssessmentValue = request.RevisedAssessmentValue;
        appeal.HearingNotes = request.HearingNotes;
        await _db.SaveChangesAsync(ct);

        await _notify.SendAppealDecisionAsync(appeal.TaxpayerId, appeal.Id, request.Decision.ToString(), ct);
        await _audit.LogAsync("DECISION", "Appeal", id, appeal.AppealCode, null, new { request.Decision, request.RevisedAssessmentValue }, true, null, ct);

        return Result<AppealDto>.Success(appeal.Adapt<AppealDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Exemptions
// ────────────────────────────────────────────────────────────────────
public class ExemptionApplicationService : IExemptionAppService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public ExemptionApplicationService(IApplicationDbContext db, IAuditService audit, ICurrentUserService user)
    { _db = db; _audit = audit; _user = user; }

    public async Task<Result<ExemptionDto>> SubmitAsync(SubmitExemptionRequest request, CancellationToken ct = default)
    {
        var exemption = request.Adapt<Exemption>();
        exemption.ExemptionCode = $"EXM-{DateTime.UtcNow.Year}-{await _db.Exemptions.CountAsync(ct) + 1:D6}";
        exemption.Status = ExemptionStatus.Submitted;
        exemption.SubmittedAt = DateTime.UtcNow;
        exemption.CreatedBy = _user.Username ?? "System";

        await _db.Exemptions.AddAsync(exemption, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("SUBMIT", "Exemption", exemption.Id, exemption.ExemptionCode, null, new { exemption.Type }, true, null, ct);

        return Result<ExemptionDto>.Success(exemption.Adapt<ExemptionDto>());
    }

    public async Task<Result<IEnumerable<ExemptionDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        var items = await _db.Exemptions.AsNoTracking().Include(e => e.Property).Include(e => e.Taxpayer).Where(e => e.PropertyId == propertyId).ToListAsync(ct);
        return Result<IEnumerable<ExemptionDto>>.Success(items.Adapt<List<ExemptionDto>>());
    }

    public async Task<Result<ExemptionDto>> ApproveAsync(Guid id, ApproveExemptionRequest request, CancellationToken ct = default)
    {
        var exemption = await _db.Exemptions.Include(e => e.Property).Include(e => e.Taxpayer).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exemption == null) return Result<ExemptionDto>.NotFound();
        if (exemption.ReviewedBy == _user.Username) return Result<ExemptionDto>.Forbidden("Reviewer cannot approve own review.");

        exemption.Status = ExemptionStatus.Approved;
        exemption.ApprovedBy = _user.Username;
        exemption.ApprovedAt = DateTime.UtcNow;
        exemption.ExemptionPercentage = request.ExemptionPercentage ?? exemption.ExemptionPercentage;
        exemption.ExemptAmount = request.ExemptAmount ?? exemption.ExemptAmount;
        exemption.ApprovalNotes = request.ApprovalNotes;
        if (request.EffectiveFrom.HasValue) exemption.EffectiveFrom = request.EffectiveFrom;
        if (request.EffectiveTo.HasValue) exemption.EffectiveTo = request.EffectiveTo;

        // Update property status
        var property = await _db.Properties.FindAsync([exemption.PropertyId], ct);
        if (property != null) property.Status = PropertyStatus.Exempt;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("APPROVE", "Exemption", id, exemption.ExemptionCode, null, new { exemption.Status, exemption.ExemptionPercentage }, true, null, ct);

        return Result<ExemptionDto>.Success(exemption.Adapt<ExemptionDto>());
    }

    public async Task<Result<ExemptionDto>> RejectAsync(Guid id, RejectExemptionRequest request, CancellationToken ct = default)
    {
        var exemption = await _db.Exemptions.Include(e => e.Property).Include(e => e.Taxpayer).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exemption == null) return Result<ExemptionDto>.NotFound();
        exemption.Status = ExemptionStatus.Rejected;
        exemption.RejectionReason = request.RejectionReason;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("REJECT", "Exemption", id, exemption.ExemptionCode, null, new { request.RejectionReason }, true, null, ct);
        return Result<ExemptionDto>.Success(exemption.Adapt<ExemptionDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Risk
// ────────────────────────────────────────────────────────────────────
public class RiskApplicationService : IRiskAppService
{
    private readonly IApplicationDbContext _db;
    private readonly IRiskScoringService _scorer;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public RiskApplicationService(IApplicationDbContext db, IRiskScoringService scorer, IAuditService audit, ICurrentUserService user)
    { _db = db; _scorer = scorer; _audit = audit; _user = user; }

    public async Task<Result<RiskScoreDto>> GetRiskScoreAsync(Guid propertyId, CancellationToken ct = default)
    {
        var latest = await _db.RiskScores.AsNoTracking().Include(r => r.Property).OrderByDescending(r => r.CalculatedAt).FirstOrDefaultAsync(r => r.PropertyId == propertyId, ct);
        if (latest == null) return await RecalculateAsync(propertyId, ct);
        return Result<RiskScoreDto>.Success(latest.Adapt<RiskScoreDto>());
    }

    public async Task<Result<RiskScoreDto>> RecalculateAsync(Guid propertyId, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Location).Include(p => p.Ownerships.Where(o => o.IsCurrent)).Include(p => p.SourceRecords.Where(r => !r.IsDeleted))
            .Include(p => p.FieldSurveys).Include(p => p.Valuations).Include(p => p.TaxBills)
            .FirstOrDefaultAsync(p => p.Id == propertyId, ct);
        if (property == null) return Result<RiskScoreDto>.NotFound();

        var result = await _scorer.ComputeRiskScoreAsync(property, ct);

        var riskScore = new RiskScore
        {
            PropertyId = propertyId,
            Level = result.Level,
            Score = result.OverallScore,
            DataCompletenessScore = result.DataCompletenessScore,
            ValuationConsistencyScore = result.ValuationConsistencyScore,
            OwnershipChainScore = result.OwnershipChainScore,
            PaymentHistoryScore = result.PaymentHistoryScore,
            GeoVerificationScore = result.GeoVerificationScore,
            SourceConsistencyScore = result.SourceConsistencyScore,
            RiskFactors = System.Text.Json.JsonSerializer.Serialize(result.RiskFactors),
            Recommendations = System.Text.Json.JsonSerializer.Serialize(result.Recommendations),
            CalculatedAt = DateTime.UtcNow,
            CalculatedBy = _user.Username ?? "System",
            CreatedBy = _user.Username ?? "System"
        };

        await _db.RiskScores.AddAsync(riskScore, ct);
        await _db.SaveChangesAsync(ct);

        return Result<RiskScoreDto>.Success(riskScore.Adapt<RiskScoreDto>());
    }

    public async Task<Result<PagedResult<FraudFlagDto>>> GetFraudFlagsAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.FraudFlags.AsNoTracking().Include(f => f.Property).Include(f => f.RaisedByUser);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(f => f.CreatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Result<PagedResult<FraudFlagDto>>.Success(new PagedResult<FraudFlagDto> { Items = items.Adapt<List<FraudFlagDto>>(), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<FraudFlagDto>> CreateFraudFlagAsync(CreateFraudFlagRequest request, CancellationToken ct = default)
    {
        var flag = request.Adapt<FraudFlag>();
        flag.RaisedByUserId = _user.UserId;
        flag.Status = FraudFlagStatus.Open;
        flag.CreatedBy = _user.Username ?? "System";

        await _db.FraudFlags.AddAsync(flag, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE_FRAUD_FLAG", "FraudFlag", flag.Id, null, null, new { flag.FlagType, flag.Severity }, true, null, ct);

        return Result<FraudFlagDto>.Success(flag.Adapt<FraudFlagDto>());
    }
}

// ────────────────────────────────────────────────────────────────────
// Integrations
// ────────────────────────────────────────────────────────────────────
public class IntegrationAppService : IIntegrationService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public IntegrationAppService(IApplicationDbContext db, IAuditService audit, ICurrentUserService user)
    { _db = db; _audit = audit; _user = user; }

    public async Task<Result<IntegrationReceiveResultDto>> ReceiveAsync(string entityCode, ReceiveIntegrationDataRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ExternalEntities.FirstOrDefaultAsync(e => e.Code == entityCode && e.IsActive, ct);
        if (entity == null) return Result<IntegrationReceiveResultDto>.NotFound($"External entity '{entityCode}' not found or inactive.");

        var integrationRequest = new IntegrationRequest
        {
            ExternalEntityId = entity.Id,
            Direction = IntegrationDirection.Inbound,
            Status = IntegrationRequestStatus.Queued,
            OperationType = request.OperationType,
            RequestPayload = request.Payload,
            QueuedAt = DateTime.UtcNow,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString(),
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            CreatedBy = _user.Username ?? "System"
        };

        await _db.IntegrationRequests.AddAsync(integrationRequest, ct);
        await _db.SaveChangesAsync(ct);
        // TODO: Enqueue Hangfire background job to process the integration request

        return Result<IntegrationReceiveResultDto>.Success(new IntegrationReceiveResultDto { RequestId = integrationRequest.Id, Accepted = true, Message = "Request queued for processing.", CorrelationId = integrationRequest.CorrelationId });
    }

    public async Task<Result<PagedResult<IntegrationRequestDto>>> GetRequestsAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.IntegrationRequests.AsNoTracking().Include(r => r.ExternalEntity);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(r => r.QueuedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Result<PagedResult<IntegrationRequestDto>>.Success(new PagedResult<IntegrationRequestDto> { Items = items.Adapt<List<IntegrationRequestDto>>(), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<bool>> RetryAsync(Guid requestId, CancellationToken ct = default)
    {
        var req = await _db.IntegrationRequests.FindAsync([requestId], ct);
        if (req == null) return Result<bool>.NotFound();
        req.Status = IntegrationRequestStatus.RetryScheduled;
        req.NextRetryAt = DateTime.UtcNow.AddSeconds(30);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────────────────────────
// Admin
// ────────────────────────────────────────────────────────────────────
public class AdminAppService : IAdminService
{
    private readonly IApplicationDbContext _db;

    public AdminAppService(IApplicationDbContext db) { _db = db; }

    public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(QueryParameters query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(l => (l.Username != null && l.Username.ToLower().Contains(term)) || l.Action.ToLower().Contains(term) || l.EntityType.ToLower().Contains(term));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(l => l.Timestamp).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto> { Items = items.Adapt<List<AuditLogDto>>(), TotalCount = total, Page = query.Page, PageSize = query.PageSize });
    }

    public async Task<Result<DashboardKpiDto>> GetDashboardKpisAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var kpi = new DashboardKpiDto
        {
            TotalProperties = await _db.Properties.CountAsync(ct),
            PropertiesDraft = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Draft, ct),
            PropertiesVerified = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Verified, ct),
            PropertiesTaxable = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Taxable, ct),
            PropertiesExempt = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Exempt, ct),
            TotalTaxpayers = await _db.Taxpayers.CountAsync(ct),
            ActiveBills = await _db.TaxBills.CountAsync(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid || b.Status == BillStatus.Overdue, ct),
            TotalBilledThisYear = await _db.TaxBills.Where(b => b.TaxYear == year).SumAsync(b => (decimal?)b.TotalAmount, ct) ?? 0,
            TotalCollectedThisYear = await _db.Payments.Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == year && p.Status == PaymentStatus.Completed).SumAsync(p => (decimal?)p.Amount, ct) ?? 0,
            PendingAppeals = await _db.Appeals.CountAsync(a => a.Status == AppealStatus.Submitted || a.Status == AppealStatus.UnderReview || a.Status == AppealStatus.Assigned, ct),
            PendingExemptions = await _db.Exemptions.CountAsync(e => e.Status == ExemptionStatus.Submitted || e.Status == ExemptionStatus.PendingApproval, ct),
            OpenFraudFlags = await _db.FraudFlags.CountAsync(f => f.Status == FraudFlagStatus.Open || f.Status == FraudFlagStatus.UnderInvestigation, ct),
            HighRiskProperties = await _db.RiskScores.CountAsync(r => (r.Level == RiskLevel.High || r.Level == RiskLevel.Critical) && r.CalculatedAt > DateTime.UtcNow.AddDays(-30), ct),
            UnmatchedSourceRecords = await _db.PropertySourceRecords.CountAsync(r => !r.IsMatched && !r.IsRejectedForMatch, ct),
            DataQualityIssues = await _db.DataQualityIssues.CountAsync(i => i.Status == "Open", ct),
            AsOf = DateTime.UtcNow
        };

        kpi.CollectionRate = kpi.TotalBilledThisYear > 0
            ? Math.Round(kpi.TotalCollectedThisYear / kpi.TotalBilledThisYear * 100, 2) : 0;

        return Result<DashboardKpiDto>.Success(kpi);
    }
}
