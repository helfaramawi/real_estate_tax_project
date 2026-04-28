using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RealEstateTax.Application.Common.Interfaces;

namespace RealEstateTax.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Principal?.FindFirstValue("sub");
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name)
                            ?? Principal?.FindFirstValue("unique_name");

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                         ?? Principal?.FindFirstValue("email");

    public IEnumerable<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public IEnumerable<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value) ?? [];

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? _httpContextAccessor.HttpContext?.TraceIdentifier;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
