using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Inventory;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Inventario: ingresos, egresos, transferencias, kardex y existencias.
/// Roles: Warehouse o Admin. "Roles" separados por coma = OR (basta con tener uno).
/// Equivalente en Spring: @PreAuthorize("hasAnyRole('WAREHOUSE','ADMIN')") a nivel de clase.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Warehouse) + "," + nameof(UserRole.Admin))]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService) => _inventoryService = inventoryService;

    /// <summary>Ingreso de mercadería: suma stock en la bodega y registra un movimiento "In".</summary>
    [HttpPost("entries")]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> RegisterEntry(StockEntryRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.RegisterEntryAsync(request, cancellationToken));

    /// <summary>Egreso de mercadería: 409 si no hay stock suficiente.</summary>
    [HttpPost("exits")]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> RegisterExit(StockExitRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.RegisterExitAsync(request, cancellationToken));

    /// <summary>Transferencia entre bodegas: resta en origen y suma en destino en la misma operación.</summary>
    [HttpPost("transfers")]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> Transfer(StockTransferRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.TransferAsync(request, cancellationToken));

    /// <summary>Kardex paginado. Ej: ?productId=1&amp;warehouseId=2&amp;type=Out</summary>
    [HttpGet("movements")]
    [ProducesResponseType<PagedResult<MovementResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovementResponse>>> GetMovements([FromQuery] MovementQuery query, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetMovementsAsync(query, cancellationToken));

    [HttpGet("movements/{id:int}")]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovementResponse>> GetMovementById(int id, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetMovementByIdAsync(id, cancellationToken));

    /// <summary>Existencias por bodega/producto. Ej: ?warehouseId=2&amp;name=arroz</summary>
    [HttpGet("stock")]
    [ProducesResponseType<PagedResult<StockResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StockResponse>>> GetStock([FromQuery] StockQuery query, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetStockAsync(query, cancellationToken));

    private CreatedAtActionResult CreatedMovement(MovementResponse movement)
        => CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
}
