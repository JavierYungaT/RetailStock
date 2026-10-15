namespace RetailStock.Application.Sap;

// DTOs que imitan el formato JSON del SAP Business One Service Layer (OData v3/v4).
// Los nombres de propiedad son EXACTAMENTE los de SAP (PascalCase: CardCode, DocDate...): el cliente
// HTTP los serializa sin cambiar a camelCase. Equivalente en Spring: DTOs con @JsonProperty("CardCode").

/// <summary>POST /b1s/v1/Login</summary>
public sealed record SapLoginRequest(string CompanyDB, string UserName, string Password);

/// <summary>Respuesta del Login. SessionTimeout en minutos (por defecto 30). También llega como cookie B1SESSION.</summary>
public sealed record SapSession(string SessionId, string? Version, int SessionTimeout);

/// <summary>
/// POST /b1s/v1/Invoices (objeto OINV de SAP).
/// NumAtCard = referencia del cliente: guardamos el número de factura de RetailStock (FAC-000001)
/// y lo usamos para detectar si la factura ya existe (idempotencia).
/// </summary>
public sealed record SapInvoiceRequest
{
    public required string CardCode { get; init; }
    /// <summary>Fecha contable "yyyy-MM-dd".</summary>
    public required string DocDate { get; init; }
    public required string DocDueDate { get; init; }
    public required string NumAtCard { get; init; }
    public string? Comments { get; init; }
    public required IReadOnlyList<SapDocumentLine> DocumentLines { get; init; }
}

/// <summary>Línea del documento (INV1). UnitPrice sin IVA: SAP calcula el impuesto con TaxCode.</summary>
public sealed record SapDocumentLine
{
    public required string ItemCode { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public string? WarehouseCode { get; init; }
    public string? TaxCode { get; init; }
}

/// <summary>Lo que nos interesa de la factura creada: DocEntry (clave interna) y DocNum (número visible).</summary>
public sealed record SapInvoiceResult(int DocEntry, int DocNum);

/// <summary>GET /b1s/v1/Items('ABR-001') (maestro de artículos OITM).</summary>
public sealed record SapItem(string ItemCode, string? ItemName, decimal QuantityOnStock);

/// <summary>Resultado de una corrida de sincronización.</summary>
public sealed record SapSyncResult(
    int Processed,
    int Succeeded,
    int Failed,
    IReadOnlyList<SapSyncItemResult> Items);

public sealed record SapSyncItemResult(
    int SaleId,
    string SaleNumber,
    bool Success,
    int? SapDocEntry,
    bool AlreadyExistedInSap,
    string? Error);
