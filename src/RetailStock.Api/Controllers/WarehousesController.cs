using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>CRUD de bodegas. DELETE físico, con 409 si la bodega tiene stock, movimientos o ventas.</summary>
[ApiController]
[Authorize] // todos los endpoints exigen token; los GET quedan para cualquier rol autenticado
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService) => _warehouseService = warehouseService;

    [HttpGet]
    [ProducesResponseType<PagedResult<WarehouseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<WarehouseResponse>>> GetAll([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => Ok(await _warehouseService.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _warehouseService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseResponse>> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseResponse>> Update(int id, UpdateWarehouseRequest request, CancellationToken cancellationToken)
        => Ok(await _warehouseService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
