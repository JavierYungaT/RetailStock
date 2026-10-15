using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Products;

/// <summary>Casos de uso de Product.</summary>
public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetPagedAsync(ProductQuery query, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Borrado lógico: marca IsActive = false (el producto sigue en ventas e historial).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
