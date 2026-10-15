using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Categories;

/// <summary>
/// Puerto de persistencia de Category: Application define QUÉ necesita, Infrastructure decide CÓMO (EF Core).
/// Equivalente en Spring: interface CategoryRepository extends JpaRepository&lt;Category, Integer&gt;,
/// pero aquí la escribimos explícita y solo con los métodos que el caso de uso necesita (Interface Segregation).
/// </summary>
public interface ICategoryRepository
{
    /// <summary>Con tracking: para modificar la entidad y luego guardar.</summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sin tracking (solo lectura).</summary>
    Task<PagedResult<Category>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default);

    void Add(Category category);
    void Remove(Category category);
}
