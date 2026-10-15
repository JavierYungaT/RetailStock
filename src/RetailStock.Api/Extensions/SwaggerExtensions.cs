using Microsoft.OpenApi.Models;

namespace RetailStock.Api.Extensions;

/// <summary>
/// Swagger con soporte para Bearer token (botón "Authorize").
/// Equivalente en springdoc: @SecurityScheme(type = HTTP, scheme = "bearer", bearerFormat = "JWT").
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RetailStock API",
                Version = "v1",
                Description = "Sistema de inventario y ventas para retail. " +
                              "1) POST /api/Auth/login  2) copia el token  3) botón Authorize (sin escribir 'Bearer')."
            });

            // Muestra los comentarios /// de los controladores en la UI de Swagger
            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
            options.IncludeXmlComments(xmlFile);

            // 1) Define el esquema de seguridad: HTTP Bearer con formato JWT
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Pega solo el token JWT; Swagger agrega el prefijo 'Bearer '.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            options.AddSecurityDefinition("Bearer", bearerScheme);

            // 2) Lo aplica a todas las operaciones: Swagger enviará "Authorization: Bearer <token>"
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [bearerScheme] = Array.Empty<string>()
            });
        });

        return services;
    }
}
