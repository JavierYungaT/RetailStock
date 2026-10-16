using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using RetailStock.Application.Auth;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Users;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Autenticación: login (público), registro (solo Admin) y datos del usuario actual.
/// Equivalente en Spring: un @RestController /api/auth con endpoints permitAll() / hasRole("ADMIN").
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, IUserService userService, ICurrentUser currentUser)
    {
        _authService = authService;
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Inicia sesión y devuelve el JWT. Usuarios de prueba:
    /// admin@retailstock.com / Admin123! · bodega@retailstock.com / Warehouse123! · caja@retailstock.com / Cashier123!
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous] // único endpoint de negocio público (como .permitAll())
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));

    /// <summary>Registra un usuario con rol Admin, Warehouse o Cashier. Solo Admin (equivale a POST /api/users).</summary>
    [HttpPost("register")]
    [Authorize(Roles = nameof(UserRole.Admin))] // como @PreAuthorize("hasRole('ADMIN')")
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        // 201 sin header Location: aún no existe un endpoint GET /api/users/{id}
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Datos del usuario autenticado leídos de los claims del token (no consulta la BD).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        // "exp" viene en segundos Unix (estándar JWT)
        DateTime? expiresAt = long.TryParse(User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value, out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
            : null;

        return Ok(new CurrentUserResponse(_currentUser.UserId!.Value, _currentUser.Email!, _currentUser.Role!, expiresAt));
    }
}
