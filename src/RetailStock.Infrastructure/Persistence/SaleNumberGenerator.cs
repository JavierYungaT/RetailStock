using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Sales;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// Números de factura con una SEQUENCE de SQL Server: "NEXT VALUE FOR" es atómico, así que dos cajeros
/// nunca obtienen el mismo número (con SELECT MAX(Number)+1 sí podría pasar).
/// Costo: si la venta hace rollback, ese número se pierde (hueco en la numeración), porque las secuencias
/// no participan de la transacción. Equivalente en JPA: @GeneratedValue(strategy = SEQUENCE).
/// </summary>
internal sealed class SaleNumberGenerator : ISaleNumberGenerator
{
    public const string SequenceName = "SaleNumbers";
    private const string Prefix = "FAC-";

    private readonly AppDbContext _context;

    public SaleNumberGenerator(AppDbContext context) => _context = context;

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        // SqlQueryRaw<T> para escalares (EF 8) requiere que la columna se llame "Value".
        // Se usa ToListAsync (no SingleAsync) para que EF no envuelva el SQL en una subconsulta:
        // SQL Server no permite NEXT VALUE FOR dentro de subconsultas.
        var values = await _context.Database
            .SqlQueryRaw<long>($"SELECT NEXT VALUE FOR dbo.{SequenceName} AS [Value]")
            .ToListAsync(cancellationToken);

        return $"{Prefix}{values.Single():D6}"; // 7 -> FAC-000007
    }
}
