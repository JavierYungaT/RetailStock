// Program.cs: punto de entrada y configuración de la aplicación.
// Equivalente en Spring Boot: la clase @SpringBootApplication con main() + las clases @Configuration.
// Tiene dos partes: (1) registrar servicios en el contenedor DI, (2) armar el pipeline HTTP (middlewares).

using Microsoft.OpenApi.Models;
using RetailStock.Api.Middleware;
using RetailStock.Application;
using RetailStock.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1) Servicios (contenedor de inyección de dependencias) ----------

builder.Services.AddControllers(); // como @RestController + Jackson

// Respuestas de error estándar RFC 7807 (ProblemDetails) también para 404/405 sin cuerpo, etc.
builder.Services.AddProblemDetails();

// Cada capa registra lo suyo; Program.cs solo las compone
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Swagger / OpenAPI (equivalente a springdoc-openapi)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RetailStock API",
        Version = "v1",
        Description = "Sistema de inventario y ventas para retail"
    });
    // Muestra los comentarios /// de los controladores en la UI de Swagger
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    options.IncludeXmlComments(xmlFile);
    // Fase 4: aquí se agrega la definición de seguridad Bearer (botón "Authorize")
});

var app = builder.Build();

// ---------- 2) Pipeline HTTP (el orden importa, como la cadena de filtros de Spring Security) ----------

// PRIMERO: así su try/catch envuelve a todos los middlewares y controladores siguientes (equivale a @ControllerAdvice)
app.UseMiddleware<GlobalExceptionMiddleware>();
// Convierte respuestas vacías 4xx/5xx (p. ej. 404 de ruta inexistente) en ProblemDetails
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "RetailStock API");
}

app.UseHttpsRedirection();
// Fase 4: app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite referenciar Program desde pruebas de integración (WebApplicationFactory<Program>)
public partial class Program;
