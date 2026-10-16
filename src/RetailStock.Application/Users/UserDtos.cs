using System.ComponentModel.DataAnnotations;
using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Enums;

namespace RetailStock.Application.Users;

// La creación usa RegisterRequest (Auth/AuthDtos.cs) y la respuesta UserResponse.

/// <summary>Filtros del listado: "name" busca en el email.</summary>
public sealed record UserQuery : PaginationQuery
{
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}

public sealed record UpdateUserRequest
{
    [Required(ErrorMessage = "El rol es obligatorio.")]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Rol inválido. Valores: Admin, Warehouse, Cashier.")]
    public UserRole? Role { get; init; }

    public bool IsActive { get; init; } = true;
}

/// <summary>El administrador asigna una contraseña nueva (p. ej. si el usuario la olvidó).</summary>
public sealed record ResetPasswordRequest
{
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).+$",
        ErrorMessage = "La contraseña debe tener mayúscula, minúscula, número y símbolo.")]
    public string NewPassword { get; init; } = string.Empty;
}
