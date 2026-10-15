using Microsoft.EntityFrameworkCore;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// DbContext = unidad de trabajo + repositorio genérico de EF Core.
/// Rastrea los cambios de las entidades cargadas y los persiste en una transacción al llamar SaveChangesAsync().
/// Equivalente en Spring: el EntityManager de JPA (con su contexto de persistencia).
/// Se registra como Scoped: una instancia por petición HTTP.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet<T> = la "tabla" consultable con LINQ (como un JpaRepository, pero sin interfaz)
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado (carpeta Configurations).
        // Así el mapeo queda fuera de las entidades y separado por entidad (Single Responsibility).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
