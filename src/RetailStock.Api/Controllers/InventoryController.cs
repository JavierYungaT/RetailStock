using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Inventory;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Inventario: ingresos, egresos, transferencias, kardex y existencias.
/// Escritura y kardex: Warehouse o Admin. Existencias: también Cashier (solo lectura, para el punto de venta).
/// "Roles" separados por coma = OR. Ojo: varios [Authorize] se combinan con AND (clase Y método), por eso los
/// roles se declaran por acción. Equivalente en Spring: @PreAuthorize("hasAnyRole(...)") en cada método.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class InventoryController : ControllerBase
{
    private const string WarehouseOrAdmin = nameof(UserRole.Warehouse) + "," + nameof(UserRole.Admin);
    private const string AnyStaff = WarehouseOrAdmin + "," + nameof(UserRole.Cashier);

    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService) => _inventoryService = inventoryService;

    /// <summary>Ingreso de mercadería: suma stock en la bodega y registra un movimiento "In".</summary>
    [HttpPost("entries")]
    [Authorize(Roles = WarehouseOrAdmin)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> RegisterEntry(StockEntryRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.RegisterEntryAsync(request, cancellationToken));

    /// <summary>Egreso de mercadería: 409 si no hay stock suficiente.</summary>
    [HttpPost("exits")]
    [Authorize(Roles = WarehouseOrAdmin)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> RegisterExit(StockExitRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.RegisterExitAsync(request, cancellationToken));

    /// <summary>Transferencia entre bodegas: resta en origen y suma en destino en la misma operación.</summary>
    [HttpPost("transfers")]
    [Authorize(Roles = WarehouseOrAdmin)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> Transfer(StockTransferRequest request, CancellationToken cancellationToken)
        => CreatedMovement(await _inventoryService.TransferAsync(request, cancellationToken));

    /// <summary>Kardex paginado. Ej: ?productId=1&amp;warehouseId=2&amp;type=Out</summary>
    [HttpGet("movements")]
    [Authorize(Roles = WarehouseOrAdmin)]
    [ProducesResponseType<PagedResult<MovementResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovementResponse>>> GetMovements([FromQuery] MovementQuery query, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetMovementsAsync(query, cancellationToken));

    [HttpGet("movements/{id:int}")]
    [Authorize(Roles = WarehouseOrAdmin)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovementResponse>> GetMovementById(int id, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetMovementByIdAsync(id, cancellationToken));

    /// <summary>Existencias por bodega/producto. Ej: ?warehouseId=2&amp;name=arroz</summary>
    [HttpGet("stock")]
    [Authorize(Roles = AnyStaff)]
    [ProducesResponseType<PagedResult<StockResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StockResponse>>> GetStock([FromQuery] StockQuery query, CancellationToken cancellationToken)
        => Ok(await _inventoryService.GetStockAsync(query, cancellationToken));

    private CreatedAtActionResult CreatedMovement(MovementResponse movement)
        => CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
}
