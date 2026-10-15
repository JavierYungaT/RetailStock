using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RetailStock.Application.Sap;

namespace RetailStock.Infrastructure.Sap;

/// <summary>
/// Simulación en memoria del Service Layer para desarrollar y probar sin un servidor SAP.
/// Se registra en DI cuando Sap:UseMock = true. Es singleton: "SAP" conserva sus facturas mientras la API corre.
///
/// Comportamiento:
///  - Conoce como artículos los códigos con prefijo ABR-, BEB- y LIM- (los del seed).
///    Una factura con otro ItemCode falla como en SAP real: error -10 "Invalid item".
///  - Asigna DocEntry/DocNum incrementales y permite buscar por NumAtCard (idempotencia).
/// </summary>
public sealed class SapServiceLayerMock : ISapServiceLayerClient
{
    private static readonly string[] KnownItemPrefixes = { "ABR-", "BEB-", "LIM-" };

    private readonly ConcurrentDictionary<string, SapInvoiceResult> _invoicesByReference = new();
    private readonly ILogger<SapServiceLayerMock> _logger;
    private int _lastDocEntry;

    public SapServiceLayerMock(ILogger<SapServiceLayerMock> logger) => _logger = logger;

    public async Task<SapSession> LoginAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // simula latencia de red
        _logger.LogInformation("[SAP MOCK] Login simulado.");
        return new SapSession($"mock-{Guid.NewGuid():N}", "1000190", 30);
    }

    public async Task<SapInvoiceResult> CreateInvoiceAsync(SapInvoiceRequest invoice, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        var unknownItem = invoice.DocumentLines.FirstOrDefault(l => !IsKnownItem(l.ItemCode));
        if (unknownItem is not null)
            throw new SapIntegrationException(
                $"SAP rechazó la operación 'crear factura {invoice.NumAtCard}' (400): Invalid item '{unknownItem.ItemCode}'",
                400, "-10");

        var docEntry = Interlocked.Increment(ref _lastDocEntry);
        var result = new SapInvoiceResult(docEntry, 50000 + docEntry);
        _invoicesByReference[invoice.NumAtCard] = result;

        _logger.LogInformation("[SAP MOCK] Factura creada: {NumAtCard} -> DocEntry {DocEntry}, {Lines} líneas, CardCode {CardCode}.",
            invoice.NumAtCard, docEntry, invoice.DocumentLines.Count, invoice.CardCode);
        return result;
    }

    public Task<SapInvoiceResult?> FindInvoiceByReferenceAsync(string numAtCard, CancellationToken cancellationToken = default)
        => Task.FromResult(_invoicesByReference.TryGetValue(numAtCard, out var result) ? result : null);

    public Task<SapItem?> GetItemAsync(string itemCode, CancellationToken cancellationToken = default)
        => Task.FromResult(IsKnownItem(itemCode)
            ? new SapItem(itemCode.ToUpperInvariant(), $"Artículo SAP {itemCode.ToUpperInvariant()}", 1000m)
            : null);

    private static bool IsKnownItem(string itemCode)
        => KnownItemPrefixes.Any(p => itemCode.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
