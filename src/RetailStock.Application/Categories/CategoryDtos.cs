using System.ComponentModel.DataAnnotations;

namespace RetailStock.Application.Categories;

// DTOs de Category. Nunca se expone la entidad: el contrato HTTP queda desacoplado del modelo de BD.
// Requests con propiedades { get; init; } para que las DataAnnotations (como Bean Validation: @NotBlank, @Size)
// se apliquen a propiedades y Swagger las muestre. [ApiController] valida y devuelve 400 automáticamente (como @Valid).

public sealed record CreateCategoryRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateCategoryRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

/// <summary>Respuesta: record posicional inmutable (solo sale de la API, no se valida).</summary>
public sealed record CategoryResponse(int Id, string Name);
