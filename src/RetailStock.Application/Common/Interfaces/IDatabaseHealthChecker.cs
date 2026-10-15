using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Puerto (interfaz) para verificar la conexión a la base de datos.
/// Application define el contrato; Infrastructure lo implementa (inversión de dependencias, la "D" de SOLID).
/// Equivalente en Spring Boot: una interfaz en el módulo de dominio/servicio
/// implementada por un @Component en la capa de infraestructura (o un HealthIndicator de Actuator).
/// </summary>
public interface IDatabaseHealthChecker
{
    Task<DatabaseHealthDto> CheckAsync(CancellationToken cancellationToken = default);
}
