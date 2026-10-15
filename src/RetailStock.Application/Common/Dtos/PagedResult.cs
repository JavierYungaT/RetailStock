namespace RetailStock.Application.Common.Dtos;

/// <summary>
/// Respuesta paginada genérica. Equivalente en Spring Data: Page&lt;T&gt; (getContent, getNumber, getTotalElements...).
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    /// <summary>Convierte los Items a otro tipo conservando los datos de paginación (entidad -> DTO).</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        => new(Items.Select(mapper).ToList(), Page, PageSize, TotalCount);
}
