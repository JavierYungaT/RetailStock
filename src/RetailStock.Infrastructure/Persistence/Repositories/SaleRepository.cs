using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Sales;
using RetailStock.Domain.Entities;

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

    public SaleRepository(AppDbContext context) => _context = context;

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

        // Rango de fechas inclusivo: [from 00:00, to+1 00:00). Comparar así (y no con .Date) permite usar IX_Sales_Date.
        if (query.From is not null)
        {
            var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
            sales = sales.Where(s => s.Date >= from);
        }
        if (query.To is not null)
        {
            var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            sales = sales.Where(s => s.Date < toExclusive);
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

    public void Add(Sale sale) => _context.Sales.Add(sale);
}
