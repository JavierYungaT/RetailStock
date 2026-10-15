namespace RetailStock.Domain.Exceptions;

/// <summary>
/// La operación choca con el estado actual de los datos (código duplicado, categoría con productos,
/// stock insuficiente, edición concurrente) -> HTTP 409 Conflict.
/// No es sealed para permitir conflictos más específicos como InsufficientStockException.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
