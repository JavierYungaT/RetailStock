using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de IWarehouseRepository.</summary>
internal sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context) => _context = context;

    public Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<PagedResult<Warehouse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var warehouses = _context.Warehouses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
            warehouses = warehouses.Where(w => w.Name.Contains(query.Name.Trim()));

        return warehouses
            .OrderBy(w => w.Name)
            .ThenBy(w => w.Id)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => _context.Warehouses.AnyAsync(w => w.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        => _context.Warehouses.AnyAsync(w => w.Name == name && w.Id != excludeId, cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Stocks.AnyAsync(s => s.WarehouseId == id, cancellationToken)
           || await _context.Sales.AnyAsync(s => s.WarehouseId == id, cancellationToken)
           || await _context.InventoryMovements.AnyAsync(m => m.FromWarehouseId == id || m.ToWarehouseId == id, cancellationToken);

    public void Add(Warehouse warehouse) => _context.Warehouses.Add(warehouse);

    public void Remove(Warehouse warehouse) => _context.Warehouses.Remove(warehouse);
}
