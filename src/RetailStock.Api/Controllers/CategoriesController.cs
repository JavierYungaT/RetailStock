using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Categories;
using RetailStock.Application.Common.Dtos;

namespace RetailStock.Api.Controllers;

/// <summary>
/// CRUD de categorías. El controlador es "delgado": recibe HTTP, delega al servicio y elige el código de respuesta.
/// No tiene try/catch: los errores (404/409) los resuelve el middleware global, y los 400 de validación
/// los genera [ApiController] automáticamente (como @Valid + MethodArgumentNotValidException).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// <summary>Lista paginada. Ej: GET /api/categories?page=1&amp;pageSize=10&amp;name=beb</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetAll([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => Ok(await _categoryService.GetPagedAsync(query, cancellationToken));

    /// <summary>":int" es una restricción de ruta: /api/categories/abc devuelve 404 sin entrar al método.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _categoryService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(request, cancellationToken);
        // 201 + header Location: /api/categories/{id}
        // (como ResponseEntity.created(uri).body(dto) en Spring)
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _categoryService.UpdateAsync(id, request, cancellationToken));

    /// <summary>409 si la categoría tiene productos.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
