namespace RetailStock.Domain.Exceptions;

/// <summary>
/// Los datos violan una regla de negocio (precio negativo, categoría inexistente...) -> HTTP 400 Bad Request.
/// </summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}
