using System.ComponentModel.DataAnnotations;

namespace RetailStock.Application.Common.Dtos;

/// <summary>
/// Parámetros de query string para listados: ?page=1&amp;pageSize=10&amp;name=arroz
/// Equivalente en Spring: Pageable + @RequestParam(required = false) String name.
/// No es "sealed" para que ProductQuery pueda extenderlo.
/// </summary>
public record PaginationQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor o igual a 1.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize { get; init; } = 10;

    /// <summary>Filtro opcional: el nombre contiene este texto (sin distinguir mayúsculas).</summary>
    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    public string? Name { get; init; }
}
