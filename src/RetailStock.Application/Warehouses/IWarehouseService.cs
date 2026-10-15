using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Warehouses;

/// <summary>Casos de uso de Warehouse.</summary>
public interface IWarehouseService
{
    Task<PagedResult<WarehouseResponse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
