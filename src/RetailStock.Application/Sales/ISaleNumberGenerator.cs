namespace RetailStock.Application.Sales;

/// <summary>
/// Genera el número de factura secuencial (FAC-000001). Infrastructure lo implementa con una SEQUENCE
/// de SQL Server, segura ante concurrencia (a diferencia de SELECT MAX(Number) + 1).
/// </summary>
public interface ISaleNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
