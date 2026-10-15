using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Inventory;

/// <summary>Puerto de persistencia de InventoryMovement (kardex). Los movimientos solo se agregan, nunca se editan.</summary>
public interface IInventoryMovementRepository
{
    Task<MovementResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<MovementResponse>> GetPagedAsync(MovementQuery query, CancellationToken cancellationToken = default);
    void Add(InventoryMovement movement);
}
