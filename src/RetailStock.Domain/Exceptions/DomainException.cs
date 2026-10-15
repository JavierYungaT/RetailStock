namespace RetailStock.Domain.Exceptions;

/// <summary>
/// Excepción base para violaciones de reglas de negocio.
/// En la Fase 3 el middleware global la traduce a una respuesta HTTP (409 Conflict).
/// Equivalente en Spring: una RuntimeException propia manejada en un @ControllerAdvice.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
