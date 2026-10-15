namespace RetailStock.Domain.Exceptions;

/// <summary>El recurso solicitado no existe -> HTTP 404 Not Found.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} con id '{key}' no existe.") { }
}
