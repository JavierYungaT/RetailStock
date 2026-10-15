using System.ComponentModel.DataAnnotations;

namespace RetailStock.Application.Reports;

/// <summary>
/// Rango de fechas de los reportes (días LOCALES del negocio, inclusivos). Ej: ?from=2026-10-01&amp;to=2026-10-31
/// </summary>
public record DateRangeQuery : IValidatableObject
{
    public const int MaxDays = 366;

    [Required(ErrorMessage = "'from' es obligatorio (formato yyyy-MM-dd).")]
    public DateOnly? From { get; init; }

    [Required(ErrorMessage = "'to' es obligatorio (formato yyyy-MM-dd).")]
    public DateOnly? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From is null || To is null)
            yield break;

        if (From > To)
            yield return new ValidationResult("'from' no puede ser mayor que 'to'.", new[] { nameof(From), nameof(To) });
        else if (To.Value.DayNumber - From.Value.DayNumber + 1 > MaxDays)
            yield return new ValidationResult($"El rango no puede superar {MaxDays} días.", new[] { nameof(From), nameof(To) });
    }
}

public sealed record SalesByDayQuery : DateRangeQuery;

public sealed record TopProductsQuery : DateRangeQuery
{
    [Range(1, 100, ErrorMessage = "'top' debe estar entre 1 y 100.")]
    public int Top { get; init; } = 10;
}

public sealed record LowStockQuery
{
    [Range(1, 1_000_000, ErrorMessage = "'threshold' debe ser mayor que 0.")]
    public int Threshold { get; init; } = 10;
}

public sealed record SalesByDayResponse(DateOnly Day, int SalesCount, decimal Total, decimal AverageTicket);

public sealed record LowStockResponse(
    int ProductId,
    string ProductCode,
    string ProductName,
    string CategoryName,
    int WarehouseId,
    string WarehouseName,
    int Quantity);

/// <summary>Revenue = ingresos sin IVA (suma de LineTotal).</summary>
public sealed record TopProductResponse(
    int ProductId,
    string ProductCode,
    string ProductName,
    int QuantitySold,
    decimal Revenue,
    int SalesCount);
