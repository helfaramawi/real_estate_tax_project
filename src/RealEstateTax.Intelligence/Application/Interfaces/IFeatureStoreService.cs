using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IFeatureStoreService
{
    Task<List<FeatureVector>> ComputeFeaturesAsync(IReadOnlyList<Guid> propertyIds, string featureVersion, CancellationToken ct = default);
    Task BulkUpsertAsync(List<FeatureVector> vectors, CancellationToken ct = default);
    Task<List<FeatureVector>> GetByVersionAsync(string featureVersion, int skip, int take, CancellationToken ct = default);
    Task<int> CountByVersionAsync(string featureVersion, CancellationToken ct = default);
}
