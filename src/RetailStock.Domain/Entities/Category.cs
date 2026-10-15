namespace RetailStock.Domain.Entities;

/// <summary>Categoría de productos (Abarrotes, Bebidas, Limpieza...). Relación 1:N con Product.</summary>
public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    // Propiedad de navegación inversa (como @OneToMany(mappedBy = "category") en JPA)
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public Category(string name) => Rename(name);

    public void Rename(string name) => Name = name.Trim();
}
