using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Products;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de IProductRepository.</summary>
internal sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        // Include = JOIN para traer la categoría (como JOIN FETCH en JPQL; evita el problema N+1)
        => _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PagedResult<Product>> GetPagedAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        // El IQueryable se va armando por partes; el SQL se ejecuta recién en CountAsync/ToListAsync
        // (equivalente a construir una Specification / Criteria dinámica en JPA)
        var products = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!query.IncludeInactive)
            products = products.Where(p => p.IsActive);

        if (query.CategoryId is not null)
            products = products.Where(p => p.CategoryId == query.CategoryId);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim();
            products = products.Where(p => p.Name.Contains(term) || p.Code.Contains(term));
        }

        return products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
        // Contains sobre una lista -> WHERE Id IN (...) en una sola consulta
        => await _context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        => _context.Products.AnyAsync(p => p.Code == code, cancellationToken);

    public void Add(Product product) => _context.Products.Add(product);
}
