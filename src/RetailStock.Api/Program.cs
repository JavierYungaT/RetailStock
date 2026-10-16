// Program.cs: punto de entrada y configuración de la aplicación.
// Equivalente en Spring Boot: la clase @SpringBootApplication con main() + las clases @Configuration.
// Tiene dos partes: (1) registrar servicios en el contenedor DI, (2) armar el pipeline HTTP (middlewares).

using System.Text.Json.Serialization;
using RetailStock.Api.BackgroundServices;
using RetailStock.Api.Extensions;
using RetailStock.Api.Middleware;
using RetailStock.Api.Services;
using RetailStock.Application;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Infrastructure;
using RetailStock.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1) Servicios (contenedor de inyección de dependencias) ----------

builder.Services.AddControllers() // como @RestController + Jackson
    // Enums como texto en JSON ("Cashier" en vez de 2), como @JsonFormat / WRITE_ENUMS_USING_TO_STRING
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Respuestas de error estándar RFC 7807 (ProblemDetails) también para 401/403/404 sin cuerpo
builder.Services.AddProblemDetails();

// Cada capa registra lo suyo; Program.cs solo las compone
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Usuario actual a partir de los claims del JWT (lo usan los servicios vía ICurrentUser)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// JWT Bearer + autorización con política "autenticado por defecto"
builder.Services.AddJwtAuthentication();

// CORS: el navegador bloquea las llamadas de http://localhost:5173 (front) a http://localhost:5080 (API)
// porque son ORÍGENES distintos (otro puerto), salvo que la API los autorice explícitamente.
// Equivalente en Spring: un CorsConfigurationSource / @CrossOrigin(origins = "http://localhost:5173").
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins) // nunca AllowAnyOrigin en producción
    .AllowAnyHeader()            // incluye Authorization y Content-Type
    .AllowAnyMethod()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10)))); // el navegador cachea el preflight OPTIONS

// Sincronización periódica de ventas con SAP (se activa con Sap:BackgroundSync:Enabled)
builder.Services.AddHostedService<SapSyncBackgroundService>();

// Swagger / OpenAPI con botón Authorize (equivalente a springdoc-openapi)
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

// En Development la API crea/actualiza la base de datos al arrancar (docker compose up deja todo listo).
// En otros entornos las migraciones se aplican como paso del despliegue, no al iniciar.
if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

// ---------- 2) Pipeline HTTP (el orden importa, como la cadena de filtros de Spring Security) ----------

// PRIMERO: así su try/catch envuelve a todos los middlewares y controladores siguientes (equivale a @ControllerAdvice)
app.UseMiddleware<GlobalExceptionMiddleware>();
// Convierte respuestas vacías 4xx/5xx (404 de ruta inexistente, 401/403 de JwtBearer) en ProblemDetails
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "RetailStock API";
        options.EnablePersistAuthorization(); // recuerda el token al recargar la página de Swagger
    });
}

app.UseHttpsRedirection();

// CORS antes de la autenticación: el preflight OPTIONS no lleva token y debe responderse sin pedirlo,
// y los 401/403 también necesitan los headers CORS para que el front pueda leerlos.
app.UseCors(FrontendCorsPolicy);

// ORDEN OBLIGATORIO: primero "¿quién eres?" (lee y valida el JWT -> HttpContext.User),
// luego "¿qué puedes hacer?" (evalúa [Authorize], roles y políticas).
// Al revés, UseAuthorization vería siempre un usuario anónimo y respondería 401 a todo.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

// Permite referenciar Program desde pruebas de integración (WebApplicationFactory<Program>)
public partial class Program;
