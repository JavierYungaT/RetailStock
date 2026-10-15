namespace RetailStock.Infrastructure.Sap;

/// <summary>
/// Guarda la sesión B1SESSION del Service Layer. Es SINGLETON porque el HttpClient tipado es transitorio
/// (se crea uno por uso) y no queremos hacer login en cada petición: la sesión dura SessionTimeout minutos.
/// El semáforo evita que 10 peticiones simultáneas hagan 10 logins a la vez.
/// </summary>
public sealed class SapSessionStore
{
    // Margen para renovar antes de que SAP la invalide
    private static readonly TimeSpan RenewalMargin = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private string? _sessionId;
    private DateTimeOffset _expiresAt;

    public SapSessionStore(TimeProvider timeProvider) => _timeProvider = timeProvider;

    public SemaphoreSlim LoginLock { get; } = new(1, 1);

    public bool TryGet(out string sessionId)
    {
        lock (_sync)
        {
            if (_sessionId is not null && _timeProvider.GetUtcNow() < _expiresAt - RenewalMargin)
            {
                sessionId = _sessionId;
                return true;
            }

            sessionId = string.Empty;
            return false;
        }
    }

    public void Set(string sessionId, TimeSpan timeout)
    {
        lock (_sync)
        {
            _sessionId = sessionId;
            _expiresAt = _timeProvider.GetUtcNow() + timeout;
        }
    }

    /// <summary>Invalida solo si sigue siendo la misma sesión (otro hilo pudo haber renovado ya).</summary>
    public void Invalidate(string sessionId)
    {
        lock (_sync)
        {
            if (_sessionId == sessionId)
                _sessionId = null;
        }
    }
}
