using Microsoft.Extensions.Logging;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Sales;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Sap;

/// <summary>
/// Sincroniza ventas pendientes como facturas en SAP Business One.
///
/// Garantías:
///  - Un fallo NO detiene el lote: se registra con ILogger y se continúa con la siguiente venta.
///  - Cada venta exitosa se marca y se guarda de inmediato (si falla la #7, las 6 anteriores ya quedaron marcadas).
///  - Idempotente: antes de crear, busca en SAP una factura con NumAtCard = número de la venta. Si el POST se
///    hizo pero no alcanzamos a marcar la venta (caída, timeout), la siguiente corrida la encuentra y solo la marca.
///  - Una sola corrida a la vez en este proceso (endpoint manual + BackgroundService no se pisan).
/// </summary>
public sealed class SapSyncService : ISapSyncService
{
    // Código de impuesto IVA 15% configurado en SAP (localización Ecuador). SAP calcula el impuesto.
    private const string TaxCode = "IVA15";

    // static: compartido por todas las instancias (el servicio es Scoped). Con varias réplicas de la API
    // haría falta un lock distribuido (p. ej. sp_getapplock en SQL Server o Redis).
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly ISaleRepository _saleRepository;
    private readonly ISapServiceLayerClient _sapClient;
    private readonly IBusinessClock _businessClock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SapSyncService> _logger;

    public SapSyncService(
        ISaleRepository saleRepository,
        ISapServiceLayerClient sapClient,
        IBusinessClock businessClock,
        IUnitOfWork unitOfWork,
        ILogger<SapSyncService> logger)
    {
        _saleRepository = saleRepository;
        _sapClient = sapClient;
        _businessClock = businessClock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SapSyncResult> SyncPendingSalesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        // WaitAsync(0): no espera; si otra corrida está activa se rechaza (409) en vez de encolarse
        if (!await SyncLock.WaitAsync(0, cancellationToken))
            throw new ConflictException("Ya hay una sincronización con SAP en curso.");

        try
        {
            var pending = await _saleRepository.GetPendingSapSyncAsync(batchSize, cancellationToken);
            if (pending.Count == 0)
            {
                _logger.LogInformation("Sincronización SAP: no hay ventas pendientes.");
                return new SapSyncResult(0, 0, 0, Array.Empty<SapSyncItemResult>());
            }

            // Fallar rápido: si SAP no responde al login, no tiene sentido intentar venta por venta
            // (SapIntegrationException -> 502 en el endpoint; el BackgroundService lo registra y reintenta luego)
            await _sapClient.LoginAsync(cancellationToken);

            var results = new List<SapSyncItemResult>(pending.Count);
            foreach (var sale in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await SyncSaleAsync(sale, cancellationToken));
            }

            var succeeded = results.Count(r => r.Success);
            _logger.LogInformation(
                "Sincronización SAP terminada: {Processed} procesadas, {Succeeded} exitosas, {Failed} fallidas.",
                results.Count, succeeded, results.Count - succeeded);

            return new SapSyncResult(results.Count, succeeded, results.Count - succeeded, results);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<SapSyncItemResult> SyncSaleAsync(Sale sale, CancellationToken cancellationToken)
    {
        try
        {
            // 1) ¿Ya existe en SAP? (reintento de una corrida que se cortó después del POST)
            var existing = await _sapClient.FindInvoiceByReferenceAsync(sale.Number, cancellationToken);
            var invoice = existing ?? await _sapClient.CreateInvoiceAsync(ToSapInvoice(sale), cancellationToken);

            // 2) Marcar y guardar YA (no al final del lote)
            sale.MarkAsSyncedToSap(invoice.DocEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Venta {SaleNumber} sincronizada con SAP: DocEntry {DocEntry}, DocNum {DocNum}{Existing}.",
                sale.Number, invoice.DocEntry, invoice.DocNum, existing is null ? "" : " (ya existía)");

            return new SapSyncItemResult(sale.Id, sale.Number, true, invoice.DocEntry, existing is not null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Se registra y se continúa con la siguiente venta. Queda SyncedToSap = false: se reintenta en la próxima corrida.
            _logger.LogError(ex, "Error al sincronizar la venta {SaleNumber} (Id {SaleId}) con SAP: {Error}",
                sale.Number, sale.Id, ex.Message);

            return new SapSyncItemResult(sale.Id, sale.Number, false, null, false, ex.Message);
        }
    }

    /// <summary>Mapeo venta de RetailStock -> factura de deudores de SAP (formato Service Layer).</summary>
    private SapInvoiceRequest ToSapInvoice(Sale sale)
    {
        var docDate = _businessClock.ToBusinessDate(sale.Date).ToString("yyyy-MM-dd");

        return new SapInvoiceRequest
        {
            // Convención de socios de negocio: "C" + cédula/RUC. En un proyecto real se consultaría/crearía
            // el BusinessPartner primero; el consumidor final suele ser un CardCode genérico.
            CardCode = $"C{sale.CustomerId}",
            DocDate = docDate,
            DocDueDate = docDate, // contado
            NumAtCard = sale.Number,
            Comments = $"RetailStock {sale.Number} - {sale.CustomerName}",
            DocumentLines = sale.Details
                .Select(d => new SapDocumentLine
                {
                    ItemCode = d.Product.Code,           // Product.Code == ItemCode de SAP
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,             // sin IVA
                    WarehouseCode = sale.WarehouseId.ToString("D2"), // WhsCode "01", "02"
                    TaxCode = TaxCode
                })
                .ToList()
        };
    }
}
