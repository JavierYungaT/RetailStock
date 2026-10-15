using RetailStock.Domain.Exceptions;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Venta (cabecera de factura). Es la "raíz del agregado": los SaleDetail solo se agregan a través de ella,
/// así Subtotal, IVA y Total siempre quedan consistentes.
/// </summary>
public class Sale
{
    /// <summary>IVA vigente en Ecuador.</summary>
    public const decimal TaxRate = 0.15m; // sufijo "m" = literal decimal (como BigDecimal, sin errores de punto flotante)

    public int Id { get; private set; }
    public string Number { get; private set; } = null!;
    public DateTime Date { get; private set; }
    public string CustomerName { get; private set; } = null!;
    /// <summary>Cédula o RUC del cliente (no es una FK).</summary>
    public string CustomerId { get; private set; } = null!;
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
    public int WarehouseId { get; private set; }
    public int UserId { get; private set; }
    public bool SyncedToSap { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;
    public User User { get; private set; } = null!;

    // Campo privado + vista de solo lectura: nadie puede hacer sale.Details.Add(...) desde fuera.
    // EF Core detecta el campo "_details" por convención y lo usa para cargar/guardar.
    private readonly List<SaleDetail> _details = new();
    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    private Sale() { }

    public Sale(string number, string customerName, string customerId, int warehouseId, int userId)
    {
        Number = number;
        CustomerName = customerName.Trim();
        CustomerId = customerId.Trim();
        WarehouseId = warehouseId;
        UserId = userId;
        Date = DateTime.UtcNow;
    }

    public void AddDetail(int productId, int quantity, decimal unitPrice)
    {
        _details.Add(new SaleDetail(productId, quantity, unitPrice));
        RecalculateTotals();
    }

    public void MarkAsSyncedToSap() => SyncedToSap = true;

    private void RecalculateTotals()
    {
        Subtotal = _details.Sum(d => d.LineTotal);
        // Redondeo comercial a 2 decimales (0.125 -> 0.13), no el "bancario" que usa .NET por defecto
        Tax = Math.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        Total = Subtotal + Tax;
    }
}
