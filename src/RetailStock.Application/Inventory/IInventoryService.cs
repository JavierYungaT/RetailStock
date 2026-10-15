using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Inventory;

/// <summary>Casos de uso de inventario: ingresos, egresos, transferencias y consultas.</summary>
public interface IInventoryService
{
    Task<MovementResponse> RegisterEntryAsync(StockEntryRequest request, CancellationToken cancellationToken = default);
    Task<MovementResponse> RegisterExitAsync(StockExitRequest request, CancellationToken cancellationToken = default);
    Task<MovementResponse> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default);

    Task<MovementResponse> GetMovementByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<MovementResponse>> GetMovementsAsync(MovementQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<StockResponse>> GetStockAsync(StockQuery query, CancellationToken cancellationToken = default);
}
