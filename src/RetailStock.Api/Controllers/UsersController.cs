using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Auth;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Users;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Gestión de usuarios (solo Admin). No hay DELETE: un usuario con ventas o movimientos no puede desaparecer;
/// se desactiva (no puede iniciar sesión) y se puede reactivar.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>Listado paginado. Ej: ?name=caja&amp;role=Cashier&amp;isActive=true</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll([FromQuery] UserQuery query, CancellationToken cancellationToken)
        => Ok(await _userService.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _userService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Cambia rol y estado. 400 si intentas desactivarte a ti mismo; 409 si quedaría sin administradores.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>El administrador asigna una contraseña nueva.</summary>
    [HttpPut("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
