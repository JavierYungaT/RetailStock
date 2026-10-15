namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Unidad de trabajo: confirma en UNA transacción todos los cambios hechos a través de los repositorios.
/// Los repositorios agregan/modifican; el servicio decide cuándo guardar.
/// Equivalente en Spring: el commit que hace @Transactional al terminar el método.
/// La implementa AppDbContext en Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
