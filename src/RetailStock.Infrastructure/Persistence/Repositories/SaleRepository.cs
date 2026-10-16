using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Sales;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Common;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación EF Core de ISaleRepository. Las lecturas usan proyecciones con Select:
///  - GetByIdAsync: cabecera + bodega + usuario + detalle con datos del producto en UNA consulta
///    (sin Select, acceder a sale.Details[i].Product en un bucle con lazy loading dispararía N+1 consultas).
///  - GetPagedAsync: solo columnas del listado; ItemsCount se calcula en SQL con un COUNT.
/// </summary>
internal sealed class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;
    private readonly TimeSpan _businessOffset;

    public SaleRepository(AppDbContext context, IOptions<BusinessSettings> businessSettings)
    {
        _context = context;
        _businessOffset = TimeSpan.FromMinutes(businessSettings.Value.UtcOffsetMinutes);
    }

    public Task<SaleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Sales
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SaleResponse(
                s.Id,
                s.Number,
                s.Date,
                s.CustomerName,
                s.CustomerId,
                s.WarehouseId,
                s.Warehouse.Name,
                s.UserId,
                s.User.Email,
                s.Subtotal,
                s.Tax,
                s.Total,
                s.SyncedToSap,
                s.SapDocEntry,
                // Colección anidada: EF la resuelve con un LEFT JOIN a SaleDetails + JOIN a Products
                s.Details
                    .OrderBy(d => d.Id)
                    .Select(d => new SaleDetailResponse(
                        d.ProductId,
                        d.Product.Code,
                        d.Product.Name,
                        d.Quantity,
                        d.UnitPrice,
                        d.LineTotal))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<SaleSummaryResponse>> GetPagedAsync(SaleQuery query, CancellationToken cancellationToken = default)
    {
        var sales = _context.Sales.AsNoTracking();

        // Rango de días LOCALES del negocio, inclusivo: [from 00:00 local, to+1 00:00 local), convertido a UTC
        // (las fechas se guardan en UTC). Comparar así, y no con .Date, permite usar IX_Sales_Date.
        if (query.From is not null)
        {
            var fromUtc = DateTime.SpecifyKind(query.From.Value.ToDateTime(TimeOnly.MinValue) - _businessOffset, DateTimeKind.Utc);
            sales = sales.Where(s => s.Date >= fromUtc);
        }
        if (query.To is not null)
        {
            var toUtcExclusive = DateTime.SpecifyKind(query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - _businessOffset, DateTimeKind.Utc);
            sales = sales.Where(s => s.Date < toUtcExclusive);
        }
        if (query.WarehouseId is not null)
            sales = sales.Where(s => s.WarehouseId == query.WarehouseId);
        if (!string.IsNullOrWhiteSpace(query.Name))
            sales = sales.Where(s => s.CustomerName.Contains(query.Name.Trim()));

        return sales
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Select(s => new SaleSummaryResponse(
                s.Id,
                s.Number,
                s.Date,
                s.CustomerName,
                s.Warehouse.Name,
                s.Details.Count, // -> subconsulta COUNT(*) en el mismo SQL, no carga los detalles
                s.Total,
                s.SyncedToSap))
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetPendingSapSyncAsync(int batchSize, CancellationToken cancellationToken = default)
        => await _context.Sales
            .Where(s => !s.SyncedToSap) // usa el índice filtrado IX_Sales_PendingSap
            .OrderBy(s => s.Id)         // las más antiguas primero
            .Take(batchSize)
            // Include + ThenInclude: detalle y producto (ItemCode) en la misma consulta, sin N+1.
            // AsSplitQuery: 2 consultas (ventas, detalles+productos) en vez de un JOIN que repite la cabecera por cada línea.
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public void Add(Sale sale) => _context.Sales.Add(sale);
}
