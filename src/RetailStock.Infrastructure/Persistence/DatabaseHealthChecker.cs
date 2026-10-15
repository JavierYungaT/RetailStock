using Microsoft.Data.SqlClient;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// Implementación de IDatabaseHealthChecker con ADO.NET puro (SqlConnection).
/// Se conecta a "master" porque RetailStockDb todavía no existe (se crea con la migración de la Fase 2).
/// Equivalente en Spring Boot: un HealthIndicator que usa un DataSource / JdbcTemplate.
/// </summary>
public sealed class DatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly string _connectionString;

    // Inyección por constructor: igual que @RequiredArgsConstructor / constructor injection en Spring.
    public DatabaseHealthChecker(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DatabaseHealthDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        try
        {
            // "await using" cierra la conexión al terminar; equivalente a try-with-resources en Java.
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // Parámetro con nombre: nunca concatenar strings en SQL (evita inyección SQL)
            command.CommandText = "SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END";
            command.Parameters.AddWithValue("@name", databaseName);
            var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;

            return new DatabaseHealthDto(true, exists, connection.ServerVersion, null);
        }
        catch (SqlException ex)
        {
            return new DatabaseHealthDto(false, false, null, ex.Message);
        }
    }
}
