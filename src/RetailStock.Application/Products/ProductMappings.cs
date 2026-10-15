using RetailStock.Domain.Entities;

namespace RetailStock.Application.Products;

/// <summary>Mapeo manual Product -> ProductResponse. Requiere que Category esté cargada (Include).</summary>
public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product)
        => new(
            product.Id,
            product.Code,
            product.Name,
            product.CategoryId,
            product.Category.Name,
            product.Price,
            product.IsActive);
}
