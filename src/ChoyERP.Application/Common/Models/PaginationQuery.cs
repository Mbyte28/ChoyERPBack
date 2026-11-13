namespace ChoyERP.Application.Common.Models;

/// <summary>
/// Parámetros comunes de paginación. La página inicial es 1 y el tamaño máximo es 100.
/// </summary>
public class PaginationQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }
}
