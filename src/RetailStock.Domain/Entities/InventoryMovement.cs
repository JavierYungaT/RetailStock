using RetailStock.Domain.Enums;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Historial (kardex) de entradas, salidas y transferencias. Es inmutable: una vez creado no se modifica.
/// In: solo ToWarehouseId · Out: solo FromWarehouseId · Transfer: ambos y distintos.
/// </summary>
public class InventoryMovement
{
    public int Id { get; private set; }
    public MovementType Type { get; private set; }
    public int ProductId { get; private set; }
    public int? FromWarehouseId { get; private set; } // "int?" = Integer nullable en Java
    public int? ToWarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime Date { get; private set; }
    public int UserId { get; private set; }

    public Product Product { get; private set; } = null!;
    public Warehouse? FromWarehouse { get; private set; }
    public Warehouse? ToWarehouse { get; private set; }
    public User User { get; private set; } = null!;

    private InventoryMovement() { }

    private InventoryMovement(MovementType type, int productId, int? fromWarehouseId, int? toWarehouseId, int quantity, int userId)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad del movimiento debe ser mayor que cero.");

        Type = type;
        ProductId = productId;
        FromWarehouseId = fromWarehouseId;
        ToWarehouseId = toWarehouseId;
        Quantity = quantity;
        UserId = userId;
        Date = DateTime.UtcNow;
    }

    // Métodos fábrica: garantizan combinaciones válidas de bodegas según el tipo
    public static InventoryMovement CreateIn(int productId, int toWarehouseId, int quantity, int userId)
        => new(MovementType.In, productId, null, toWarehouseId, quantity, userId);

    public static InventoryMovement CreateOut(int productId, int fromWarehouseId, int quantity, int userId)
        => new(MovementType.Out, productId, fromWarehouseId, null, quantity, userId);

    public static InventoryMovement CreateTransfer(int productId, int fromWarehouseId, int toWarehouseId, int quantity, int userId)
    {
        if (fromWarehouseId == toWarehouseId)
            throw new DomainException("La bodega de origen y destino deben ser distintas.");

        return new(MovementType.Transfer, productId, fromWarehouseId, toWarehouseId, quantity, userId);
    }
}
