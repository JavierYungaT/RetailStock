using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Sap;
using RetailStock.Domain.Enums;

namespace RetailStock.Api.Controllers;

/// <summary>Integración con SAP Business One (solo Admin).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SapController : ControllerBase
{
    private readonly ISapSyncService _sapSyncService;
    private readonly ISapServiceLayerClient _sapClient;

    public SapController(ISapSyncService sapSyncService, ISapServiceLayerClient sapClient)
    {
        _sapSyncService = sapSyncService;
        _sapClient = sapClient;
    }

    /// <summary>
    /// Envía a SAP las ventas con SyncedToSap = false como facturas. Responde 200 aunque algunas fallen
    /// (el detalle por venta está en "items"); 409 si ya hay una sincronización en curso; 502 si SAP no responde.
    /// </summary>
    [HttpPost("sync-sales")]
    [ProducesResponseType<SapSyncResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SapSyncResult>> SyncSales(
        [FromQuery, Range(1, 500)] int batchSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _sapSyncService.SyncPendingSalesAsync(batchSize, cancellationToken));

    /// <summary>Consulta un artículo en SAP (GET /Items('code')). Útil para verificar la conexión.</summary>
    [HttpGet("items/{itemCode}")]
    [ProducesResponseType<SapItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SapItem>> GetItem(string itemCode, CancellationToken cancellationToken)
    {
        var item = await _sapClient.GetItemAsync(itemCode, cancellationToken);
        return item is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado", detail: $"El artículo '{itemCode}' no existe en SAP.")
            : Ok(item);
    }
}
