namespace RealEstateTax.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string? entityCode,
        object? oldValues,
        object? newValues,
        bool isSuccess = true,
        string? failureReason = null,
        CancellationToken ct = default);
}
