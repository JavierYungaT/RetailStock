using Microsoft.AspNetCore.Mvc;
using RetailStock.Application.Common.Exceptions;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Api.Middleware;

/// <summary>
/// Middleware global de excepciones: envuelve TODO el pipeline en un try/catch y convierte las excepciones
/// en respuestas ProblemDetails (RFC 7807, application/problem+json) con el código HTTP correcto.
/// Equivalente en Spring: @RestControllerAdvice + @ExceptionHandler que devuelven ProblemDetail (Spring 6).
///
///   InvalidCredentialsException -> 401 Unauthorized (login fallido)
///   NotFoundException      -> 404 Not Found
///   BusinessRuleException  -> 400 Bad Request
///   ConflictException      -> 409 Conflict (incluye InsufficientStockException)
///   cualquier otra         -> 500 Internal Server Error (sin filtrar detalles internos fuera de Development)
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    // Los middlewares "por convención" se crean UNA vez (singleton): solo se inyectan dependencias singleton aquí.
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // continúa con el resto del pipeline (como chain.doFilter en un Filter de Java)
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no es un error del servidor, no hay a quién responder
            _logger.LogInformation("Petición cancelada por el cliente: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Credenciales inválidas"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Regla de negocio violada"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto con el estado actual"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        // Logging estructurado: {Placeholders} se guardan como campos, no solo texto (como MDC/SLF4J con parámetros)
        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("{ExceptionType} en {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // En 500 no se expone el mensaje real en producción (podría filtrar SQL, rutas, etc.)
            Detail = status == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                ? "Ocurrió un error inesperado. Contacte al administrador."
                : exception.Message,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.io/{status}"
        };
        // traceId permite cruzar la respuesta del cliente con la línea del log
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is InsufficientStockException stock)
        {
            problem.Extensions["productId"] = stock.ProductId;
            problem.Extensions["warehouseId"] = stock.WarehouseId;
            problem.Extensions["available"] = stock.Available;
            problem.Extensions["requested"] = stock.Requested;
        }

        // Si ya se empezó a enviar la respuesta no se pueden cambiar status ni headers
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("La respuesta ya había comenzado; no se puede escribir ProblemDetails.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
