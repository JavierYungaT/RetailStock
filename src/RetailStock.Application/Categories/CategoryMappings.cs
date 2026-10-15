using RetailStock.Domain.Entities;

namespace RetailStock.Application.Categories;

/// <summary>
/// Mapeo manual entidad -> DTO con métodos de extensión: category.ToResponse().
/// Sin AutoMapper: explícito, verificado por el compilador y fácil de depurar.
/// Equivalente en Spring: un mapper estático o una interfaz de MapStruct (pero escrito a mano).
/// </summary>
public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category)
        => new(category.Id, category.Name);
}
