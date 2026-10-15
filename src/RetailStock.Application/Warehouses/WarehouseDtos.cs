using System.ComponentModel.DataAnnotations;

namespace RetailStock.Application.Warehouses;

public sealed record CreateWarehouseRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Debe tener entre {2} y {1} caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Debe tener entre {2} y {1} caracteres.")]
    public string Location { get; init; } = string.Empty;
}

public sealed record UpdateWarehouseRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Debe tener entre {2} y {1} caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Debe tener entre {2} y {1} caracteres.")]
    public string Location { get; init; } = string.Empty;
}

public sealed record WarehouseResponse(int Id, string Name, string Location);
