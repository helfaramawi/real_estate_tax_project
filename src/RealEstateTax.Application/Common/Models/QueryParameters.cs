namespace RealEstateTax.Application.Common.Models;

public class QueryParameters
{
    private const int MaxPageSize = 200;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public bool IncludeDeleted { get; set; }
}
