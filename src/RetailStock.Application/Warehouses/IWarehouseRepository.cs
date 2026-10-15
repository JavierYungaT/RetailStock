using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Warehouses;

/// <summary>Puerto de persistencia de Warehouse.</summary>
public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<Warehouse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>True si la bodega tiene stock, movimientos o ventas (no se puede eliminar).</summary>
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);

    void Add(Warehouse warehouse);
    void Remove(Warehouse warehouse);
}
