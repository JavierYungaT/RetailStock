using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Inventory;

/// <summary>Puerto de persistencia de Stock.</summary>
public interface IStockRepository
{
    /// <summary>Con tracking: se va a modificar la cantidad (EF usará RowVersion en el UPDATE).</summary>
    Task<Stock?> GetAsync(int productId, int warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stocks de varios productos en una bodega, en UNA sola consulta (WHERE ProductId IN (...)).
    /// Evita N+1: no hacer un SELECT por cada ítem de la venta.
    /// </summary>
    Task<IReadOnlyList<Stock>> GetByWarehouseAsync(int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);

    /// <summary>Consulta de existencias proyectada a DTO (sin tracking).</summary>
    Task<PagedResult<StockResponse>> GetPagedAsync(StockQuery query, CancellationToken cancellationToken = default);

    void Add(Stock stock);
}
