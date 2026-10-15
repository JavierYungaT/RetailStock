using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Categories;

/// <summary>
/// Lógica de los casos de uso de Category. Equivalente en Spring: una clase @Service.
/// Valida reglas que necesitan consultar la BD (nombre único, categoría sin productos) y lanza
/// excepciones de dominio; el middleware global las convierte en 404/409.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CategoryResponse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _categoryRepository.GetPagedAsync(query, cancellationToken);
        return page.Map(c => c.ToResponse());
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => (await GetExistingAsync(id, cancellationToken)).ToResponse();

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, null, cancellationToken);

        var category = new Category(request.Name);
        _categoryRepository.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // aquí se ejecuta el INSERT y se asigna el Id

        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetExistingAsync(id, cancellationToken);
        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        category.Rename(request.Name);
        // No hace falta llamar a "Update": el change tracker detecta el cambio (como el dirty checking de Hibernate)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await GetExistingAsync(id, cancellationToken);

        if (await _categoryRepository.HasProductsAsync(id, cancellationToken))
            throw new ConflictException($"La categoría '{category.Name}' tiene productos asociados y no se puede eliminar.");

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetExistingAsync(int id, CancellationToken cancellationToken)
        => await _categoryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(Category), id);

    private async Task EnsureNameIsUniqueAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        if (await _categoryRepository.NameExistsAsync(name.Trim(), excludeId, cancellationToken))
            throw new ConflictException($"Ya existe una categoría con el nombre '{name.Trim()}'.");
    }
}
