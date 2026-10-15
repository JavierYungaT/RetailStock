using RetailStock.Application.Categories;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Products;

/// <summary>
/// Casos de uso de Product. Reglas que valida:
///  - El código no se repite (409 Conflict).
///  - La categoría indicada existe (400 Bad Request: el dato enviado es inválido, el recurso de la URL sí existe).
///  - DELETE es lógico (IsActive = false): un producto con ventas no puede desaparecer del historial.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _productRepository.GetPagedAsync(query, cancellationToken);
        return page.Map(p => p.ToResponse());
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => (await GetExistingAsync(id, cancellationToken)).ToResponse();

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var code = Product.NormalizeCode(request.Code);
        if (await _productRepository.CodeExistsAsync(code, cancellationToken))
            throw new ConflictException($"Ya existe un producto con el código '{code}'.");

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var product = new Product(code, request.Name, request.CategoryId, request.Price);
        _productRepository.Add(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Se recarga para traer la Category (el response incluye CategoryName)
        return (await GetExistingAsync(product.Id, cancellationToken)).ToResponse();
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingAsync(id, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        product.Update(request.Name, request.CategoryId, request.Price);
        if (request.IsActive) product.Activate(); else product.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Si cambió la categoría, la navegación cargada apunta a la anterior: se recarga
        return (await GetExistingAsync(id, cancellationToken)).ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingAsync(id, cancellationToken);
        product.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> GetExistingAsync(int id, CancellationToken cancellationToken)
        => await _productRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(Product), id);

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await _categoryRepository.ExistsAsync(categoryId, cancellationToken))
            throw new BusinessRuleException($"La categoría con id '{categoryId}' no existe.");
    }
}
