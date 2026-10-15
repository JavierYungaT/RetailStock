using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Sales;

/// <summary>Casos de uso de ventas.</summary>
public interface ISaleService
{
    Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<SaleSummaryResponse>> GetPagedAsync(SaleQuery query, CancellationToken cancellationToken = default);
}
