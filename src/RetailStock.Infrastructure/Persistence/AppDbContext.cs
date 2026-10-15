using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// DbContext = unidad de trabajo + repositorio genérico de EF Core.
/// Rastrea los cambios de las entidades cargadas y los persiste en una transacción al llamar SaveChangesAsync().
/// Equivalente en Spring: el EntityManager de JPA (con su contexto de persistencia).
/// Se registra como Scoped: una instancia por petición HTTP.
/// También implementa IUnitOfWork, el puerto que usan los servicios de Application.
/// </summary>
public sealed class AppDbContext : DbContext, IUnitOfWork
{
    // Códigos de error de SQL Server
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyViolation = 547;

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

    /// <summary>
    /// Guarda los cambios y traduce errores de BD a excepciones de dominio.
    /// Es la última red de seguridad: si dos peticiones pasan a la vez la validación de "código único",
    /// el índice UNIQUE de SQL Server rechaza la segunda y aquí se convierte en 409 en vez de 500.
    /// (Equivalente a traducir DataIntegrityViolationException en Spring).
    /// </summary>
    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // rowversion distinto: otro usuario modificó el registro (concurrencia optimista, como OptimisticLockException)
            throw new ConflictException("El registro fue modificado por otro usuario. Vuelva a intentarlo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            throw new ConflictException("Ya existe un registro con esos datos únicos.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: ForeignKeyViolation })
        {
            throw new ConflictException("La operación viola una relación con otros registros.");
        }
    }
}
