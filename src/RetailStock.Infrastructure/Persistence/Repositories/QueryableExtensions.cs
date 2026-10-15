using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>
/// Paginación reutilizable para cualquier IQueryable. Genera dos consultas:
///   SELECT COUNT(*) ...                         (total para calcular páginas)
///   SELECT ... ORDER BY ... OFFSET x ROWS FETCH NEXT y ROWS ONLY   (Skip/Take)
/// Equivalente en Spring Data: findAll(spec, PageRequest.of(page, size)).
/// </summary>
internal static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize) // la página 1 empieza en 0
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
