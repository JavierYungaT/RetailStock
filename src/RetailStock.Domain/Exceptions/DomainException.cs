namespace RetailStock.Domain.Exceptions;

/// <summary>
/// Base abstracta de todas las excepciones de negocio. Cada subclase representa un tipo de error
/// que el middleware global traduce a un código HTTP (sin que el dominio sepa nada de HTTP).
/// Equivalente en Spring: RuntimeExceptions propias manejadas por un @ControllerAdvice.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
