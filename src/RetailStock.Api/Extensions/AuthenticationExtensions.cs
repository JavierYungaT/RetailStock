using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RetailStock.Infrastructure.Authentication;

namespace RetailStock.Api.Extensions;

/// <summary>
/// Configura la autenticación JWT Bearer y la autorización.
/// Equivalente en Spring Security: SecurityFilterChain con .oauth2ResourceServer(jwt -> ...) y un JwtDecoder.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        // "Bearer" es el esquema por defecto: [Authorize] sin parámetros usa JWT
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Se configuran las opciones de JwtBearer leyendo IOptions<JwtSettings> (el mismo Options pattern
        // que usa JwtTokenService), así emisión y validación usan exactamente la misma Key/Issuer/Audience.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // No renombrar claims ("role" -> "http://schemas.microsoft.com/.../role"): se usan tal cual vienen
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    // Tolerancia por desfase de relojes entre servidores (por defecto son 5 minutos)
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    // Qué claim usa ASP.NET Core para User.Identity.Name y para [Authorize(Roles = ...)]
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = JwtTokenService.RoleClaim
                };
            });

        services.AddAuthorization(options =>
        {
            // Política de respaldo: cualquier endpoint SIN atributo de autorización exige usuario autenticado.
            // Si alguien olvida [Authorize] en un controlador nuevo, queda protegido igual ("seguro por defecto").
            // Como .anyRequest().authenticated() al final del SecurityFilterChain.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
