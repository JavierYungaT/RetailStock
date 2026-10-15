namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Unidad de trabajo: confirma en UNA transacción todos los cambios hechos a través de los repositorios.
/// Los repositorios agregan/modifican; el servicio decide cuándo guardar.
/// Equivalente en Spring: el commit que hace @Transactional al terminar el método.
/// La implementa AppDbContext en Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Un solo SaveChanges ya es atómico: EF envuelve todos sus INSERT/UPDATE en una transacción implícita.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta <paramref name="operation"/> dentro de una transacción explícita (BeginTransaction → Commit,
    /// o Rollback si lanza una excepción). Úsalo cuando la operación mezcla varias lecturas/escrituras
    /// que deben ser un todo o nada (p. ej. una venta). Equivalente a un método @Transactional en Spring.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
