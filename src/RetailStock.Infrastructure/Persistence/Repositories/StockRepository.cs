using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Inventory;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de IStockRepository.</summary>
internal sealed class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context) => _context = context;

    public Task<Stock?> GetAsync(int productId, int warehouseId, CancellationToken cancellationToken = default)
        // FindAsync busca por clave primaria (compuesta): primero en el change tracker, luego en la BD
        => _context.Stocks.FindAsync(new object[] { productId, warehouseId }, cancellationToken).AsTask();

    public async Task<IReadOnlyList<Stock>> GetByWarehouseAsync(int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default)
        => await _context.Stocks
            .Where(s => s.WarehouseId == warehouseId && productIds.Contains(s.ProductId))
            .ToListAsync(cancellationToken); // con tracking: se van a descontar

    public Task<PagedResult<StockResponse>> GetPagedAsync(StockQuery query, CancellationToken cancellationToken = default)
    {
        var stocks = _context.Stocks.AsNoTracking();

        if (query.WarehouseId is not null)
            stocks = stocks.Where(s => s.WarehouseId == query.WarehouseId);
        if (query.ProductId is not null)
            stocks = stocks.Where(s => s.ProductId == query.ProductId);
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim();
            stocks = stocks.Where(s => s.Product.Name.Contains(term) || s.Product.Code.Contains(term));
        }

        // Proyección con Select: EF genera UN SELECT con JOIN a Products y Warehouses y trae solo estas columnas.
        // No hace falta Include: al proyectar, EF resuelve las navegaciones dentro del mismo SQL (sin N+1).
        return stocks
            .OrderBy(s => s.Warehouse.Name)
            .ThenBy(s => s.Product.Name)
            .Select(s => new StockResponse(
                s.ProductId,
                s.Product.Code,
                s.Product.Name,
                s.WarehouseId,
                s.Warehouse.Name,
                s.Quantity))
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public void Add(Stock stock) => _context.Stocks.Add(stock);
}
