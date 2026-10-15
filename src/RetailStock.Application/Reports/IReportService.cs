namespace RetailStock.Application.Reports;

/// <summary>Casos de uso de reportes (solo lectura).</summary>
public interface IReportService
{
    Task<IReadOnlyList<SalesByDayResponse>> GetSalesByDayAsync(SalesByDayQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(LowStockQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(TopProductsQuery query, CancellationToken cancellationToken = default);
}
