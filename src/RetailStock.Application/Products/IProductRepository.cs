using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Products;

/// <summary>Puerto de persistencia de Product (lo implementa Infrastructure con EF Core).</summary>
public interface IProductRepository
{
    /// <summary>Con tracking e incluye Category (necesaria para armar ProductResponse).</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> GetPagedAsync(ProductQuery query, CancellationToken cancellationToken = default);

    /// <summary>Varios productos en una sola consulta (WHERE Id IN (...)), sin tracking.</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Product product);
}
