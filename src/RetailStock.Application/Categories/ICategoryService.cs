using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Categories;

/// <summary>
/// Casos de uso de Category. El controlador depende de esta interfaz (fácil de mockear en pruebas).
/// Equivalente en Spring: la interfaz de un @Service. Solo recibe y devuelve DTOs.
/// </summary>
public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
