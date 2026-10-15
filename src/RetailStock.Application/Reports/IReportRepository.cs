namespace RetailStock.Application.Reports;

/// <summary>
/// Puerto de los reportes. A Application no le importa si la implementación usa EF, Dapper o
/// procedimientos almacenados: Infrastructure lo resuelve con Dapper + SP.
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<SalesByDayResponse>> GetSalesByDayAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(DateOnly from, DateOnly to, int top, CancellationToken cancellationToken = default);
}
