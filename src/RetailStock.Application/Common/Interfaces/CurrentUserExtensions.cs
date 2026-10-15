namespace RetailStock.Application.Common.Interfaces;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Id del usuario del token. Si no hay (no debería pasar: los endpoints tienen [Authorize]),
    /// es un error de programación, no del cliente -> 500.
    /// </summary>
    public static int GetRequiredUserId(this ICurrentUser currentUser)
        => currentUser.UserId ?? throw new InvalidOperationException("La operación requiere un usuario autenticado.");
}
