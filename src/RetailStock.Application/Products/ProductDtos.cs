using System.ComponentModel.DataAnnotations;
using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Products;

// DTOs de Product. DataAnnotations = Bean Validation (@NotBlank, @Size, @Pattern, @DecimalMin).

public sealed record CreateProductRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una categoría válida.")]
    public int CategoryId { get; init; }

    // Range con double: evita problemas de cultura al parsear límites decimales ("0.01" vs "0,01")
    [Range(0.01, 999_999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999999.99.")]
    public decimal Price { get; init; }
}

/// <summary>
/// El código NO se puede modificar: es el identificador de negocio (se sincroniza con SAP como ItemCode).
/// IsActive permite reactivar un producto dado de baja.
/// </summary>
public sealed record UpdateProductRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una categoría válida.")]
    public int CategoryId { get; init; }

    [Range(0.01, 999_999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999999.99.")]
    public decimal Price { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record ProductResponse(
    int Id,
    string Code,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal Price,
    bool IsActive);

/// <summary>Filtros del listado de productos: hereda paginación y nombre, agrega categoría e inactivos.</summary>
public sealed record ProductQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    /// <summary>Por defecto solo se listan productos activos (los eliminados lógicamente se ocultan).</summary>
    public bool IncludeInactive { get; init; }
}
