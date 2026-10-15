// Program.cs: punto de entrada y configuración de la aplicación.
// Equivalente en Spring Boot: la clase @SpringBootApplication con main() + las clases @Configuration.
// Tiene dos partes: (1) registrar servicios en el contenedor DI, (2) armar el pipeline HTTP (middlewares).

using Microsoft.OpenApi.Models;
using RetailStock.Application;
using RetailStock.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1) Servicios (contenedor de inyección de dependencias) ----------

builder.Services.AddControllers(); // como @RestController + Jackson

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
    // Fase 4: aquí se agrega la definición de seguridad Bearer (botón "Authorize")
});

var app = builder.Build();

// ---------- 2) Pipeline HTTP (el orden importa, como la cadena de filtros de Spring Security) ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "RetailStock API");
}

// Fase 3: middleware global de excepciones (equivalente a @ControllerAdvice)
app.UseHttpsRedirection();
// Fase 4: app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite referenciar Program desde pruebas de integración (WebApplicationFactory<Program>)
public partial class Program;
