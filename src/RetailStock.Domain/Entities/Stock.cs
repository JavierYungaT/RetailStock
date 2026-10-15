using RetailStock.Domain.Exceptions;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Existencias de un producto en una bodega. Clave compuesta (ProductId, WarehouseId):
/// equivalente a @IdClass / @EmbeddedId en JPA.
/// Aquí vive la regla de negocio principal: no se puede descontar más de lo disponible.
/// </summary>
public class Stock
{
    public int ProductId { get; private set; }
    public int WarehouseId { get; private set; }
    public int Quantity { get; private set; }

    // Token de concurrencia (rowversion en SQL Server): si dos ventas modifican el mismo stock
    // a la vez, la segunda falla en vez de pisar a la primera. Equivalente a @Version en JPA.
    public byte[] RowVersion { get; private set; } = null!;

    public Product Product { get; private set; } = null!;
    public Warehouse Warehouse { get; private set; } = null!;

    private Stock() { }

    public Stock(int productId, int warehouseId, int quantity = 0)
    {
        if (quantity < 0)
            throw new BusinessRuleException("La cantidad inicial no puede ser negativa.");

        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = quantity;
    }

    public void Increase(int quantity)
    {
        EnsurePositive(quantity);
        Quantity += quantity;
    }

    public void Decrease(int quantity)
    {
        EnsurePositive(quantity);
        if (quantity > Quantity)
            throw new InsufficientStockException(ProductId, WarehouseId, Quantity, quantity);

        Quantity -= quantity;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0)
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");
    }
}
