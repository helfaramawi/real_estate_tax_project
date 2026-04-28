using System.Text.Json;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IApplicationDbContext db, ICurrentUserService currentUser, ILogger<AuditService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string? entityCode,
        object? oldValues,
        object? newValues,
        bool isSuccess = true,
        string? failureReason = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username,
                UserRole = string.Join(",", _currentUser.Roles.Take(3)),
                IpAddress = _currentUser.IpAddress,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityCode = entityCode,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                CorrelationId = _currentUser.CorrelationId,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                Timestamp = DateTime.UtcNow
            };

            await _db.AuditLogs.AddAsync(log, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit failures must never break business flows
            _logger.LogError(ex, "Failed to write audit log for action {Action} on {EntityType} {EntityId}", action, entityType, entityId);
        }
    }
}
