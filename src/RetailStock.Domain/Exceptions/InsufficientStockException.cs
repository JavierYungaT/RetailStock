namespace RetailStock.Domain.Exceptions;

/// <summary>Regla de negocio: no se puede vender ni egresar más stock del disponible -> HTTP 409.</summary>
public sealed class InsufficientStockException : ConflictException
{
    public int ProductId { get; }
    public int WarehouseId { get; }
    public int Available { get; }
    public int Requested { get; }

    public InsufficientStockException(int productId, int warehouseId, int available, int requested)
        : base($"Stock insuficiente para el producto {productId} en la bodega {warehouseId}: disponible {available}, solicitado {requested}.")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        Available = available;
        Requested = requested;
    }
}
