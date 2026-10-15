namespace RetailStock.Application.Sap;

/// <summary>
/// Puerto hacia SAP Business One Service Layer. Application define QUÉ necesita de SAP;
/// Infrastructure decide CÓMO (HttpClient real o mock). Es la inversión de dependencias aplicada a
/// un sistema externo: el negocio no conoce URLs, cookies ni JSON de SAP.
/// Equivalente en Spring: una interfaz implementada con RestClient/WebClient (o un @FeignClient).
/// </summary>
public interface ISapServiceLayerClient
{
    /// <summary>Abre sesión (POST /Login). El cliente la gestiona solo; llamarla explícitamente sirve para "fallar rápido".</summary>
    Task<SapSession> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea una factura de deudores (POST /Invoices). NO es idempotente: llamarla dos veces crea dos facturas.</summary>
    Task<SapInvoiceResult> CreateInvoiceAsync(SapInvoiceRequest invoice, CancellationToken cancellationToken = default);

    /// <summary>Busca una factura por NumAtCard (GET /Invoices?$filter=NumAtCard eq '...'). Null si no existe.</summary>
    Task<SapInvoiceResult?> FindInvoiceByReferenceAsync(string numAtCard, CancellationToken cancellationToken = default);

    /// <summary>Consulta un artículo (GET /Items('code')). Null si no existe en SAP.</summary>
    Task<SapItem?> GetItemAsync(string itemCode, CancellationToken cancellationToken = default);
}
