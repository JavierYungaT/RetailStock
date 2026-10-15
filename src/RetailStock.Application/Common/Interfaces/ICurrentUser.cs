namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Usuario de la petición actual, leído de los claims del JWT.
/// Los servicios lo usan para saber QUIÉN hace la operación (UserId de ventas y movimientos en la Fase 5)
/// sin depender de HttpContext. Equivalente en Spring: SecurityContextHolder.getContext().getAuthentication().
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}
