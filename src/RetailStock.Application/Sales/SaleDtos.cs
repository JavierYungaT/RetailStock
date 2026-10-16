using System.ComponentModel.DataAnnotations;
using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Sales;

/// <summary>
/// Venta a registrar. El precio NO viene del cliente: se toma del catálogo (nunca confiar en el frontend).
/// </summary>
public sealed record CreateSaleRequest
{
    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Debe tener entre {2} y {1} caracteres.")]
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Cédula (10 dígitos) o RUC (13 dígitos). Consumidor final: 9999999999999.</summary>
    [Required(ErrorMessage = "La identificación del cliente es obligatoria.")]
    [RegularExpression(@"^\d{10}(\d{3})?$", ErrorMessage = "La identificación debe ser una cédula (10 dígitos) o RUC (13 dígitos).")]
    public string CustomerId { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una bodega válida.")]
    public int WarehouseId { get; init; }

    // [ApiController] valida también cada SaleItemRequest de la lista (validación anidada, como @Valid en List<@Valid T>)
    [Required]
    [MinLength(1, ErrorMessage = "La venta debe tener al menos un ítem.")]
    [MaxLength(100, ErrorMessage = "La venta admite máximo 100 ítems.")]
    public List<SaleItemRequest> Items { get; init; } = new();
}

public sealed record SaleItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un producto válido.")]
    public int ProductId { get; init; }

    [Range(1, 10_000, ErrorMessage = "La cantidad debe estar entre 1 y 10.000.")]
    public int Quantity { get; init; }
}

/// <summary>Venta completa con su detalle (GET por id y respuesta del POST).</summary>
public sealed record SaleResponse(
    int Id,
    string Number,
    DateTime Date,
    string CustomerName,
    string CustomerId,
    int WarehouseId,
    string WarehouseName,
    int UserId,
    string UserEmail,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    bool SyncedToSap,
    int? SapDocEntry,
    IReadOnlyList<SaleDetailResponse> Details);

public sealed record SaleDetailResponse(
    int ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Fila del listado (sin el detalle, para que el listado sea liviano).</summary>
public sealed record SaleSummaryResponse(
    int Id,
    string Number,
    DateTime Date,
    string CustomerName,
    string WarehouseName,
    int ItemsCount,
    decimal Total,
    bool SyncedToSap);

/// <summary>
/// Filtros del listado: ?from=2026-10-01&amp;to=2026-10-15&amp;warehouseId=1&amp;name=juan
/// Fechas inclusivas en días LOCALES del negocio (Business:UtcOffsetMinutes). "name" filtra por nombre del cliente.
/// IValidatableObject = validación entre campos (como un @AssertTrue o un ConstraintValidator de clase).
/// </summary>
public sealed record SaleQuery : PaginationQuery, IValidatableObject
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    [Range(1, int.MaxValue)]
    public int? WarehouseId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From is not null && To is not null && From > To)
            yield return new ValidationResult("'from' no puede ser mayor que 'to'.", new[] { nameof(From), nameof(To) });
    }
}
