namespace RetailStock.Application.Sap;

/// <summary>
/// Error devuelto por SAP o de comunicación con SAP. El middleware lo traduce a 502 Bad Gateway:
/// nuestra API funciona, pero el sistema del que depende falló.
/// SapErrorCode = el "code" del JSON de error del Service Layer (p. ej. -10 = artículo inexistente).
/// </summary>
public sealed class SapIntegrationException : Exception
{
    public int? HttpStatusCode { get; }
    public string? SapErrorCode { get; }

    public SapIntegrationException(string message, int? httpStatusCode = null, string? sapErrorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        SapErrorCode = sapErrorCode;
    }
}
