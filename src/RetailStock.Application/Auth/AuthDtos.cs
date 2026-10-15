using System.ComponentModel.DataAnnotations;
using RetailStock.Domain.Enums;

namespace RetailStock.Application.Auth;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}

public sealed record RegisterRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).+$",
        ErrorMessage = "La contraseña debe tener mayúscula, minúscula, número y símbolo.")]
    public string Password { get; init; } = string.Empty;

    /// <summary>Admin, Warehouse o Cashier (se recibe como texto gracias a JsonStringEnumConverter).</summary>
    [Required]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Rol inválido. Valores: Admin, Warehouse, Cashier.")]
    public UserRole? Role { get; init; }
}

/// <summary>Respuesta del login: el cliente guarda el token y lo envía en "Authorization: Bearer ...".</summary>
public sealed record LoginResponse(string Token, string TokenType, DateTime ExpiresAtUtc, string Email, string Role);

/// <summary>Usuario sin datos sensibles (nunca se devuelve el PasswordHash).</summary>
public sealed record UserResponse(int Id, string Email, string Role);

/// <summary>Datos del usuario autenticado, obtenidos de los claims del token (sin consultar la BD).</summary>
public sealed record CurrentUserResponse(int Id, string Email, string Role, DateTime? TokenExpiresAtUtc);
