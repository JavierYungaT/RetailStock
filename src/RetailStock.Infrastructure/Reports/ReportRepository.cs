using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using RetailStock.Application.Reports;
using RetailStock.Infrastructure.Persistence;

namespace RetailStock.Infrastructure.Reports;

/// <summary>
/// Reportes con Dapper + procedimientos almacenados. Dapper es un "micro-ORM": ejecuta el SQL que le das
/// y mapea cada fila a un objeto por nombre de columna, sin change tracking ni traducción de LINQ.
/// Equivalente en Spring: JdbcTemplate / SimpleJdbcCall con un RowMapper (pero el mapeo es automático).
/// </summary>
internal sealed class ReportRepository : IReportRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ReportSettings _settings;

    public ReportRepository(SqlConnectionFactory connectionFactory, IOptions<ReportSettings> settings)
    {
        _connectionFactory = connectionFactory;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<SalesByDayResponse>> GetSalesByDayAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        // DbType.Date explícito: el parámetro llega como DATE (igual que en el SP) y no como datetime2/nvarchar
        parameters.Add("@From", from.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("@To", to.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("@UtcOffsetMinutes", _settings.UtcOffsetMinutes, DbType.Int32);

        // "await using": la conexión se cierra (vuelve al pool) al salir del método.
        // Dapper la abre sola si está cerrada.
        await using var connection = _connectionFactory.Create();

        // La columna DATE llega como DateTime: se mapea a una fila intermedia y luego a DateOnly
        var rows = await connection.QueryAsync<SalesByDayRow>(new CommandDefinition(
            "dbo.sp_SalesByDay",
            parameters,
            commandType: CommandType.StoredProcedure, // ejecuta el SP por nombre (como SimpleJdbcCall)
            cancellationToken: cancellationToken));

        return rows
            .Select(r => new SalesByDayResponse(DateOnly.FromDateTime(r.Day), r.SalesCount, r.Total, r.AverageTicket))
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();

        // Objeto anónimo como parámetros: Dapper crea @Threshold a partir de la propiedad
        var rows = await connection.QueryAsync<LowStockResponse>(new CommandDefinition(
            "dbo.sp_LowStock",
            new { Threshold = threshold },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.AsList(); // AsList evita copiar si Dapper ya devolvió una List<T>
    }

    public async Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(DateOnly from, DateOnly to, int top, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@From", from.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("@To", to.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("@Top", top, DbType.Int32);
        parameters.Add("@UtcOffsetMinutes", _settings.UtcOffsetMinutes, DbType.Int32);

        await using var connection = _connectionFactory.Create();

        // Dapper mapea cada fila al constructor del record (por nombre de columna = nombre de parámetro)
        var rows = await connection.QueryAsync<TopProductResponse>(new CommandDefinition(
            "dbo.sp_TopProducts",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private sealed class SalesByDayRow
    {
        public DateTime Day { get; init; }
        public int SalesCount { get; init; }
        public decimal Total { get; init; }
        public decimal AverageTicket { get; init; }
    }
}
