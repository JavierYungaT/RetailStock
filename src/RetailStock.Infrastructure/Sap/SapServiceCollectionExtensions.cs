using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using RetailStock.Application.Sap;

namespace RetailStock.Infrastructure.Sap;

/// <summary>
/// Registro de la integración con SAP. Según "Sap:UseMock" se registra el mock o el cliente real:
/// el resto de la aplicación (SapSyncService, controladores) no se entera del cambio.
/// Equivalente en Spring: @ConditionalOnProperty(name = "sap.use-mock", havingValue = "true").
/// </summary>
internal static class SapServiceCollectionExtensions
{
    public static IServiceCollection AddSapIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SapSettings.SectionName);

        services.AddOptions<SapSettings>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(s => s.UseMock || s.HasRealConnectionSettings(),
                "Con Sap:UseMock = false se requieren Sap:BaseUrl, CompanyDB, UserName y Password.")
            .ValidateOnStart();

        var settings = section.Get<SapSettings>() ?? new SapSettings();

        if (settings.UseMock)
        {
            services.AddSingleton<ISapServiceLayerClient, SapServiceLayerMock>();
            return services;
        }

        services.AddSingleton<SapSessionStore>();

        // HttpClient TIPADO: IHttpClientFactory crea el HttpClient (reutilizando los handlers del pool,
        // lo que evita agotar sockets) y lo inyecta en SapServiceLayerClient.
        services.AddHttpClient<ISapServiceLayerClient, SapServiceLayerClient>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + "/");
                // El timeout real lo controla la política de resiliencia; este es solo un tope superior
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                UseCookies = false,                               // la cookie B1SESSION se envía a mano
                PooledConnectionLifetime = TimeSpan.FromMinutes(5) // respeta cambios de DNS
                // Nota: el Service Layer suele usar certificado autofirmado; en producción se instala la CA,
                // nunca se desactiva la validación del certificado.
            })
            // Polly (Microsoft.Extensions.Http.Resilience): pipeline estándar =
            // rate limiter -> timeout total -> REINTENTOS con backoff exponencial y jitter -> circuit breaker -> timeout por intento.
            // Solo reacciona a errores TRANSITORIOS: 408, 429, 5xx, HttpRequestException y timeouts (no a 400/401/404).
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1); // 1s, 2s, 4s (+ jitter)
                // POST /Invoices NO es idempotente: si SAP creó la factura pero la respuesta se perdió,
                // reintentarlo duplicaría la factura. Solo se reintentan métodos seguros (GET/HEAD/OPTIONS)
                // ante errores transitorios. La idempotencia de las facturas la garantiza SapSyncService con NumAtCard.
                options.Retry.ShouldHandle = args =>
                {
                    var method = args.Context.GetRequestMessage()?.Method;
                    var isSafeMethod = method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;
                    return ValueTask.FromResult(isSafeMethod && HttpClientResiliencePredicates.IsTransient(args.Outcome));
                };
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(Math.Min(settings.TimeoutSeconds, 30));
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                // Circuit breaker: si falla >= 50% de al menos 10 peticiones en 60 s, "abre" 30 s
                // y falla de inmediato sin golpear a un SAP que ya está caído.
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.MinimumThroughput = 10;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
