using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Sales;

/// <summary>
/// Puerto de persistencia de Sale. Las lecturas devuelven DTOs proyectados con Select
/// (solo las columnas necesarias, en una consulta, sin N+1 ni tracking).
/// </summary>
public interface ISaleRepository
{
    Task<SaleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<SaleSummaryResponse>> GetPagedAsync(SaleQuery query, CancellationToken cancellationToken = default);
    void Add(Sale sale);
}
