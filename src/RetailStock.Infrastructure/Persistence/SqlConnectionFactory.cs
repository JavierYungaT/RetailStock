using Microsoft.Data.SqlClient;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// Crea conexiones ADO.NET para Dapper. Cada repositorio abre su conexión, la usa y la cierra ("using").
/// No hay que preocuparse por el costo: SqlClient mantiene un POOL de conexiones; "cerrar" la devuelve al pool.
/// Equivalente en Spring: el DataSource (HikariCP) que usa JdbcTemplate.
/// </summary>
internal sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public SqlConnection Create() => new(_connectionString);
}
