using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Sales;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Ventas. Roles: Cashier o Admin. El cajero que registra la venta se toma del token, no del body.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Cashier) + "," + nameof(UserRole.Admin))]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService) => _saleService = saleService;

    /// <summary>
    /// Registra una venta: valida stock, calcula IVA 15%, genera FAC-XXXXXX, descuenta stock y registra
    /// movimientos en una sola transacción. 409 si algún producto no tiene stock suficiente.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var sale = await _saleService.CreateSaleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    /// <summary>Venta con su detalle de productos.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _saleService.GetByIdAsync(id, cancellationToken));

    /// <summary>Listado paginado. Ej: ?from=2026-10-01&amp;to=2026-10-31&amp;warehouseId=1 (fechas en UTC, inclusivas).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SaleSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SaleSummaryResponse>>> GetAll([FromQuery] SaleQuery query, CancellationToken cancellationToken)
        => Ok(await _saleService.GetPagedAsync(query, cancellationToken));
}
