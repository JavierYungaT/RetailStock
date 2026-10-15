using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Products;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// CRUD de productos. DELETE es lógico (IsActive = false).
/// GET: cualquier usuario autenticado · POST/PUT/DELETE: solo Admin (403 para Warehouse/Cashier).
/// </summary>
[ApiController]
[Authorize] // todos los endpoints exigen token; los GET quedan para cualquier rol autenticado
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    /// <summary>
    /// Lista paginada con filtros. Ej: GET /api/products?name=arroz&amp;categoryId=1&amp;includeInactive=false
    /// "name" busca en nombre y código.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll([FromQuery] ProductQuery query, CancellationToken cancellationToken)
        => Ok(await _productService.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _productService.GetByIdAsync(id, cancellationToken));

    /// <summary>400 si los datos son inválidos o la categoría no existe; 409 si el código ya existe.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken)
        => Ok(await _productService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Borrado lógico: el producto queda inactivo pero conserva su historial de ventas.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
