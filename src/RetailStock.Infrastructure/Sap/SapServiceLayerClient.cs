using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RetailStock.Application.Sap;

namespace RetailStock.Infrastructure.Sap;

/// <summary>
/// Cliente HTTP real del SAP Business One Service Layer ("HttpClient tipado": recibe un HttpClient
/// ya configurado por IHttpClientFactory con BaseAddress, timeout y la política de resiliencia de Polly).
/// Equivalente en Spring: un @Component con RestClient/WebClient construido por un RestClient.Builder.
///
/// Sesión: POST /Login devuelve SessionId (y la cookie B1SESSION). Cada petición envía
/// "Cookie: B1SESSION=...". Si SAP responde 401 (sesión expirada o reiniciada), se hace login
/// otra vez y se reintenta la petición UNA vez.
/// </summary>
public sealed class SapServiceLayerClient : ISapServiceLayerClient
{
    // El Service Layer usa PascalCase (CardCode, DocDate): sin política de nombres = nombres tal cual.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly SapSettings _settings;
    private readonly SapSessionStore _sessionStore;
    private readonly ILogger<SapServiceLayerClient> _logger;

    public SapServiceLayerClient(
        HttpClient httpClient,
        IOptions<SapSettings> settings,
        SapSessionStore sessionStore,
        ILogger<SapServiceLayerClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<SapSession> LoginAsync(CancellationToken cancellationToken = default)
    {
        // Nunca registrar la contraseña en logs
        _logger.LogInformation("Login en SAP Service Layer: CompanyDB {CompanyDB}, usuario {UserName}.",
            _settings.CompanyDB, _settings.UserName);

        var body = new SapLoginRequest(_settings.CompanyDB, _settings.UserName, _settings.Password);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("Login", CreateJsonContent(body), cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new SapIntegrationException($"No se pudo conectar con SAP Service Layer: {ex.Message}", innerException: ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, "Login", cancellationToken);
            var session = await response.Content.ReadFromJsonAsync<SapSession>(JsonOptions, cancellationToken)
                          ?? throw new SapIntegrationException("SAP devolvió un login vacío.");

            _sessionStore.Set(session.SessionId, TimeSpan.FromMinutes(session.SessionTimeout > 0 ? session.SessionTimeout : 30));
            return session;
        }
    }

    public async Task<SapInvoiceResult> CreateInvoiceAsync(SapInvoiceRequest invoice, CancellationToken cancellationToken = default)
    {
        using var response = await SendWithSessionAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "Invoices") { Content = CreateJsonContent(invoice) },
            cancellationToken);

        await EnsureSuccessAsync(response, $"crear factura {invoice.NumAtCard}", cancellationToken);
        return await response.Content.ReadFromJsonAsync<SapInvoiceResult>(JsonOptions, cancellationToken)
               ?? throw new SapIntegrationException("SAP no devolvió la factura creada.");
    }

    public async Task<SapInvoiceResult?> FindInvoiceByReferenceAsync(string numAtCard, CancellationToken cancellationToken = default)
    {
        // OData: comillas simples escapadas duplicándolas; el valor se codifica para la URL
        var filter = Uri.EscapeDataString($"NumAtCard eq '{numAtCard.Replace("'", "''")}'");
        using var response = await SendWithSessionAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"Invoices?$select=DocEntry,DocNum&$filter={filter}"),
            cancellationToken);

        await EnsureSuccessAsync(response, $"buscar factura {numAtCard}", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ODataCollection<SapInvoiceResult>>(JsonOptions, cancellationToken);
        return result?.Value.FirstOrDefault();
    }

    public async Task<SapItem?> GetItemAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        var key = Uri.EscapeDataString(itemCode.Replace("'", "''"));
        using var response = await SendWithSessionAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"Items('{key}')?$select=ItemCode,ItemName,QuantityOnStock"),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"consultar artículo {itemCode}", cancellationToken);
        return await response.Content.ReadFromJsonAsync<SapItem>(JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Envía con la cookie de sesión; ante 401 renueva la sesión y reintenta una vez.
    /// Recibe una FÁBRICA de requests porque un HttpRequestMessage no se puede enviar dos veces.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithSessionAsync(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        var sessionId = await GetSessionIdAsync(cancellationToken);
        var response = await SendAsync(createRequest(), sessionId, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        _logger.LogWarning("SAP respondió 401 (sesión expirada). Renovando sesión y reintentando.");
        response.Dispose();
        _sessionStore.Invalidate(sessionId);

        sessionId = await GetSessionIdAsync(cancellationToken);
        return await SendAsync(createRequest(), sessionId, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string sessionId, CancellationToken cancellationToken)
    {
        using (request)
        {
            // Cookie manual (UseCookies = false en el handler): el CookieContainer se compartiría
            // entre todos los clientes del pool y no controlaríamos cuándo se renueva.
            request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            try
            {
                return await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new SapIntegrationException($"Error de comunicación con SAP: {ex.Message}", innerException: ex);
            }
        }
    }

    private async Task<string> GetSessionIdAsync(CancellationToken cancellationToken)
    {
        if (_sessionStore.TryGet(out var sessionId))
            return sessionId;

        // Doble verificación: si varios hilos llegan a la vez, solo el primero hace login
        await _sessionStore.LoginLock.WaitAsync(cancellationToken);
        try
        {
            if (_sessionStore.TryGet(out sessionId))
                return sessionId;

            return (await LoginAsync(cancellationToken)).SessionId;
        }
        finally
        {
            _sessionStore.LoginLock.Release();
        }
    }

    /// <summary>
    /// JSON ya serializado (StringContent) para que la petición lleve Content-Length.
    /// PostAsJsonAsync/JsonContent envían "Transfer-Encoding: chunked", que algunos proxies y balanceadores
    /// delante del Service Layer no aceptan.
    /// </summary>
    private static StringContent CreateJsonContent<T>(T value)
        => new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    /// <summary>
    /// Traduce el error del Service Layer: {"error": {"code": -10, "message": {"lang": "en-us", "value": "..."}}}
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string? sapCode = null;
        var message = body;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("code", out var code))
                    sapCode = code.ToString();
                if (error.TryGetProperty("message", out var msg))
                    message = msg.ValueKind == JsonValueKind.Object && msg.TryGetProperty("value", out var value)
                        ? value.GetString() ?? body
                        : msg.ToString();
            }
        }
        catch (JsonException)
        {
            // Cuerpo no JSON (p. ej. HTML de un proxy): se usa tal cual
        }

        throw new SapIntegrationException(
            $"SAP rechazó la operación '{operation}' ({(int)response.StatusCode}): {message}",
            (int)response.StatusCode,
            sapCode);
    }

    private sealed record ODataCollection<T>(IReadOnlyList<T> Value);
}
