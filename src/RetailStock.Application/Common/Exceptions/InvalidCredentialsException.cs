namespace RetailStock.Application.Common.Exceptions;

/// <summary>
/// Login fallido -> HTTP 401. El mensaje es genérico a propósito: no revela si el email existe
/// (evita enumeración de usuarios). Equivalente en Spring: BadCredentialsException.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Email o contraseña incorrectos.") { }
}
