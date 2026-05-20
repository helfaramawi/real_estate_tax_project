using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IRouteOptimizationService
{
    Task<RouteAssignment> OptimizeRouteAsync(OptimizeRouteDto request, CancellationToken ct = default);
    Task<List<RouteAssignment>> GetAssignmentsAsync(Guid inspectorId, DateOnly date, CancellationToken ct = default);
    Task<RouteAssignment?> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<List<InspectorTrack>> GetTrackAsync(Guid assignmentId, CancellationToken ct = default);
}
