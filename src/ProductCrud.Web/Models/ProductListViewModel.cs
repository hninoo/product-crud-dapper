namespace ProductCrud.Web.Models;

public sealed class ProductListViewModel
{
    public static readonly int[] AllowedPageSizes = [5, 10, 15, 20];

    public IReadOnlyList<Product> Products { get; init; } = [];
    public string? SearchTerm { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalCount);
}
