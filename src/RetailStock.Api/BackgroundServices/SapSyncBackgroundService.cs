using Microsoft.Extensions.Options;
using RetailStock.Application.Sap;
using RetailStock.Domain.Exceptions;
using RetailStock.Infrastructure.Sap;

namespace RetailStock.Api.BackgroundServices;

/// <summary>
/// Tarea en segundo plano que sincroniza las ventas pendientes cada N minutos (Sap:BackgroundSync).
/// Equivalente en Spring: un método @Scheduled(fixedDelay = 300000) en un @Component.
///
/// Detalles importantes:
///  - BackgroundService es SINGLETON y SapSyncService es SCOPED (depende del DbContext): se crea un
///    scope nuevo en cada ciclo, igual que cada petición HTTP tiene el suyo.
///  - Nunca deja escapar una excepción: en .NET 8 una excepción no controlada en un BackgroundService
///    DETIENE toda la aplicación.
///  - PeriodicTimer no acumula ejecuciones: si un ciclo tarda más que el intervalo, el siguiente espera.
/// </summary>
public sealed class SapSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SapBackgroundSyncSettings _settings;
    private readonly ILogger<SapSyncBackgroundService> _logger;

    public SapSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SapSettings> options,
        ILogger<SapSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value.BackgroundSync;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Sincronización automática con SAP desactivada (Sap:BackgroundSync:Enabled = false).");
            return;
        }

        _logger.LogInformation("Sincronización automática con SAP cada {Minutes} min, lotes de {BatchSize}.",
            _settings.IntervalMinutes, _settings.BatchSize);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.IntervalMinutes));
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISapSyncService>();
            await syncService.SyncPendingSalesAsync(_settings.BatchSize, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo
        }
        catch (ConflictException)
        {
            _logger.LogInformation("Sincronización SAP omitida: ya hay otra en curso.");
        }
        catch (Exception ex)
        {
            // SAP caído, BD no disponible... se registra y se reintenta en el próximo ciclo
            _logger.LogError(ex, "Falló la sincronización automática con SAP; se reintentará en {Minutes} min.", _settings.IntervalMinutes);
        }
    }
}
