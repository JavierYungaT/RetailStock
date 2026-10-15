namespace RetailStock.Domain.Entities;

/// <summary>Bodega o sucursal donde se almacena stock.</summary>
public class Warehouse
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Location { get; private set; } = null!;

    public ICollection<Stock> Stocks { get; private set; } = new List<Stock>();

    private Warehouse() { }

    public Warehouse(string name, string location) => Update(name, location);

    public void Update(string name, string location)
    {
        Name = name.Trim();
        Location = location.Trim();
    }
}
