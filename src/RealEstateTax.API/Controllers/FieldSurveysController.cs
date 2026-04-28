using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.FieldSurveys;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.API.Controllers;

[ApiController]
[Route("api/field-surveys")]
[Authorize]
[Produces("application/json")]
public class FieldSurveysController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;

    public FieldSurveysController(IApplicationDbContext db, IFileStorageService storage, IAuditService audit, ICurrentUserService user)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters query, CancellationToken ct)
    {
        IQueryable<FieldSurvey> q = _db.FieldSurveys
            .Include(s => s.Property)
            .Include(s => s.AssignedToUser);

        if (_user.IsInRole("FieldInspector") && _user.UserId.HasValue)
            q = q.Where(s => s.AssignedToUserId == _user.UserId);

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(s => s.SurveyCode.Contains(term) || s.Property.PropertyCode.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            data = new { items = items.Adapt<List<FieldSurveyDto>>(), total, query.Page, query.PageSize }
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> Create([FromBody] CreateFieldSurveyRequest request, CancellationToken ct)
    {
        var survey = request.Adapt<FieldSurvey>();
        survey.SurveyCode = $"SRV-{DateTime.UtcNow.Year}-{await _db.FieldSurveys.CountAsync(ct) + 1:D6}";
        survey.Status = SurveyStatus.Assigned;
        survey.AssignedAt = DateTime.UtcNow;
        survey.CreatedBy = _user.Username ?? "System";

        await _db.FieldSurveys.AddAsync(survey, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE", "FieldSurvey", survey.Id, survey.SurveyCode, null, new { request.PropertyId, request.AssignedToUserId }, true, null, ct);

        return Ok(new { success = true, data = survey.Adapt<FieldSurveyDto>() });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer,FieldInspector")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFieldSurveyRequest request, CancellationToken ct)
    {
        var survey = await _db.FieldSurveys.FindAsync([id], ct);
        if (survey == null) return NotFound();

        if (_user.IsInRole("FieldInspector") && survey.AssignedToUserId != _user.UserId)
            return Forbid();

        request.Adapt(survey);
        if (survey.Status == SurveyStatus.Assigned)
            survey.Status = SurveyStatus.InProgress;
        if (!survey.StartedAt.HasValue)
            survey.StartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = survey.Adapt<FieldSurveyDetailDto>() });
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "FieldInspector,Admin,SuperAdmin")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var survey = await _db.FieldSurveys.FindAsync([id], ct);
        if (survey == null) return NotFound();

        survey.Status = SurveyStatus.Submitted;
        survey.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("SUBMIT", "FieldSurvey", id, survey.SurveyCode, null, null, true, null, ct);

        return Ok(new { success = true, message = "Survey submitted." });
    }

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "FieldInspector,Admin,SuperAdmin")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(
        Guid id,
        IFormFile file,
        [FromForm] string? description,
        [FromForm] string? attachmentType,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Unsupported file type. Allowed: JPEG, PNG, WebP, PDF." });

        var survey = await _db.FieldSurveys.FindAsync([id], ct);
        if (survey == null) return NotFound();

        await using var stream = file.OpenReadStream();
        var path = await _storage.UploadAsync(stream, file.FileName, file.ContentType, $"surveys/{id}", ct);

        var attachment = new SurveyAttachment
        {
            SurveyId = id,
            FileName = file.FileName,
            StoragePath = path,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AttachmentType = attachmentType,
            Description = description,
            CapturedAt = DateTime.UtcNow,
            CreatedBy = _user.Username ?? "System"
        };

        await _db.SurveyAttachments.AddAsync(attachment, ct);
        await _db.SaveChangesAsync(ct);

        var dto = attachment.Adapt<SurveyAttachmentDto>();
        dto.FileUrl = _storage.GetPublicUrl(path);
        return Ok(new { success = true, data = dto });
    }
}
