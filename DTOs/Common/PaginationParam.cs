namespace BackendApp.DTOs.Common;

public class PaginationParam
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageIndex { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 10 : value);
    }
}
