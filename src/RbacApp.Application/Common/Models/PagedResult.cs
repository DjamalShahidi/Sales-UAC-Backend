namespace RbacApp.Application.Common.Models;

/// <summary>
/// نتیجه‌ی صفحه‌بندی‌شده برای queryهای لیستی.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public record PageQuery(int Page = 1, int PageSize = 20, string? Search = null)
{
    public int Skip => (Math.Max(1, Page) - 1) * PageSize;
}
