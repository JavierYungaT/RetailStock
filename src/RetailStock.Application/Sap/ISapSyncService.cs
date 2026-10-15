namespace RetailStock.Application.Sap;

/// <summary>Envía a SAP las ventas pendientes (SyncedToSap = false).</summary>
public interface ISapSyncService
{
    Task<SapSyncResult> SyncPendingSalesAsync(int batchSize, CancellationToken cancellationToken = default);
}
