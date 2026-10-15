using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Categories;
using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación EF Core de ICategoryRepository. Equivalente en Spring: la implementación que Spring Data
/// genera para un JpaRepository, pero escrita a mano con LINQ (que EF traduce a SQL).
/// No llama a SaveChanges: eso lo decide el servicio vía IUnitOfWork.
/// </summary>
internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<PagedResult<Category>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: solo lectura, EF no guarda copias para detectar cambios -> menos memoria y más rápido
        var categories = _context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
            categories = categories.Where(c => c.Name.Contains(query.Name.Trim())); // -> LIKE '%texto%'

        // Siempre ordenar antes de paginar: sin ORDER BY el orden de SQL Server no está garantizado
        return categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => _context.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        => _context.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, cancellationToken);

    public Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default)
        => _context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken);

    public void Add(Category category) => _context.Categories.Add(category);

    public void Remove(Category category) => _context.Categories.Remove(category);
}
