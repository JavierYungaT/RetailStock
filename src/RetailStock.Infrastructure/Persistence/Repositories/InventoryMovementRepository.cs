using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Inventory;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de IInventoryMovementRepository (kardex).</summary>
internal sealed class InventoryMovementRepository : IInventoryMovementRepository
{
    // Proyección reutilizable: Expression (no Func) para que EF la traduzca a SQL en vez de ejecutarla en memoria.
    // Las navegaciones opcionales (FromWarehouse/ToWarehouse) se traducen a LEFT JOIN.
    private static readonly Expression<Func<InventoryMovement, MovementResponse>> ToResponse = m => new MovementResponse(
        m.Id,
        m.Type,
        m.ProductId,
        m.Product.Code,
        m.Product.Name,
        m.FromWarehouseId,
        m.FromWarehouse != null ? m.FromWarehouse.Name : null,
        m.ToWarehouseId,
        m.ToWarehouse != null ? m.ToWarehouse.Name : null,
        m.Quantity,
        m.Date,
        m.UserId,
        m.User.Email);

    private readonly AppDbContext _context;

    public InventoryMovementRepository(AppDbContext context) => _context = context;

    public Task<MovementResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<MovementResponse>> GetPagedAsync(MovementQuery query, CancellationToken cancellationToken = default)
    {
        var movements = _context.InventoryMovements.AsNoTracking();

        if (query.ProductId is not null)
            movements = movements.Where(m => m.ProductId == query.ProductId);
        if (query.WarehouseId is not null)
            movements = movements.Where(m => m.FromWarehouseId == query.WarehouseId || m.ToWarehouseId == query.WarehouseId);
        if (query.Type is not null)
            movements = movements.Where(m => m.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.Name))
            movements = movements.Where(m => m.Product.Name.Contains(query.Name.Trim()));

        return movements
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .Select(ToResponse)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public void Add(InventoryMovement movement) => _context.InventoryMovements.Add(movement);
}
