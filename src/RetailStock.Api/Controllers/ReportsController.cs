using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Reports;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Reportes gerenciales (solo Admin). Se ejecutan con Dapper sobre procedimientos almacenados.
/// Las fechas son días locales del negocio (Business:UtcOffsetMinutes).
/// </summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService) => _reportService = reportService;

    /// <summary>Número de ventas, total y ticket promedio por día. Ej: ?from=2026-10-01&amp;to=2026-10-31</summary>
    [HttpGet("sales-by-day")]
    [ProducesResponseType<IReadOnlyList<SalesByDayResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SalesByDayResponse>>> GetSalesByDay([FromQuery] SalesByDayQuery query, CancellationToken cancellationToken)
        => Ok(await _reportService.GetSalesByDayAsync(query, cancellationToken));

    /// <summary>Productos activos con stock menor al umbral, por bodega. Ej: ?threshold=10</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType<IReadOnlyList<LowStockResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<LowStockResponse>>> GetLowStock([FromQuery] LowStockQuery query, CancellationToken cancellationToken)
        => Ok(await _reportService.GetLowStockAsync(query, cancellationToken));

    /// <summary>Productos más vendidos por cantidad (e ingresos sin IVA). Ej: ?from=2026-10-01&amp;to=2026-10-31&amp;top=5</summary>
    [HttpGet("top-products")]
    [ProducesResponseType<IReadOnlyList<TopProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopProductResponse>>> GetTopProducts([FromQuery] TopProductsQuery query, CancellationToken cancellationToken)
        => Ok(await _reportService.GetTopProductsAsync(query, cancellationToken));
}
