using RetailStock.Domain.Exceptions;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Línea de una venta. Guarda UnitPrice "congelado": si el producto cambia de precio después,
/// la factura histórica no se altera.
/// </summary>
public class SaleDetail
{
    public int Id { get; private set; }
    public int SaleId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    public Sale Sale { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private SaleDetail() { }

    // "internal": solo Sale (mismo ensamblado) puede crear detalles
    internal SaleDetail(int productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad vendida debe ser mayor que cero.");
        if (unitPrice < 0)
            throw new DomainException("El precio unitario no puede ser negativo.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }
}
