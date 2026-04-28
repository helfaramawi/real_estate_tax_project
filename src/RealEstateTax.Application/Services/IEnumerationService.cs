using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Enumeration;

namespace RealEstateTax.Application.Services;

public interface IEnumerationService
{
    Task<Result<ImportResultDto>> ImportSourceRecordsAsync(ImportSourceRecordsRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<MatchResultDto>>> MatchSourceRecordsAsync(MatchSourceRecordsRequest request, CancellationToken ct = default);
    Task<Result<Guid>> CreateMasterRecordAsync(CreateMasterRecordRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<UnmatchedRecordDto>>> GetUnmatchedRecordsAsync(QueryParameters query, CancellationToken ct = default);
    Task<Result<PagedResult<DataQualityIssueDto>>> GetDataQualityIssuesAsync(QueryParameters query, CancellationToken ct = default);
}
