using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;

namespace RetailStock.Api.Controllers;

/// <summary>
/// Endpoints de salud para verificar que la API y SQL Server responden.
/// [ApiController] + ControllerBase = @RestController en Spring.
/// [Route("api/[controller]")] = @RequestMapping("/api/health") ("[controller]" toma el nombre sin el sufijo "Controller").
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthChecker _databaseHealthChecker;

    // El controlador depende de la interfaz (Application), no de la implementación (Infrastructure)
    public HealthController(IDatabaseHealthChecker databaseHealthChecker)
    {
        _databaseHealthChecker = databaseHealthChecker;
    }

    /// <summary>GET /api/health: la API está viva.</summary>
    [HttpGet] // = @GetMapping
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "Healthy", timestampUtc = DateTime.UtcNow });

    /// <summary>GET /api/health/db: verifica la conexión a SQL Server.</summary>
    [HttpGet("db")]
    [ProducesResponseType<DatabaseHealthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<DatabaseHealthDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DatabaseHealthDto>> GetDatabase(CancellationToken cancellationToken)
    {
        // async/await: libera el hilo mientras espera a la BD (similar a CompletableFuture, pero con sintaxis lineal)
        var result = await _databaseHealthChecker.CheckAsync(cancellationToken);
        return result.CanConnect ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
