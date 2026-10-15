using RetailStock.Domain.Exceptions;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Producto del catálogo. Los setters son privados: el estado solo cambia mediante métodos
/// que validan las reglas (entidad "rica" en vez de un simple contenedor de datos).
/// </summary>
public class Product
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int CategoryId { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }

    // Navegaciones: "= null!" le dice al compilador que EF siempre las cargará cuando se usen
    public Category Category { get; private set; } = null!;
    public ICollection<Stock> Stocks { get; private set; } = new List<Stock>();

    private Product() { }

    public Product(string code, string name, int categoryId, decimal price)
    {
        Code = NormalizeCode(code);
        IsActive = true;
        Update(name, categoryId, price);
    }

    /// <summary>Formato canónico del código (" abr-001 " -> "ABR-001"); se usa también para buscar duplicados.</summary>
    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public void Update(string name, int categoryId, decimal price)
    {
        if (price < 0)
            throw new BusinessRuleException("El precio no puede ser negativo.");

        Name = name.Trim();
        CategoryId = categoryId;
        Price = price;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
