using System.ComponentModel.DataAnnotations;

namespace RetailStock.Infrastructure.Authentication;

/// <summary>
/// Sección "Jwt" de appsettings mapeada a una clase tipada (Options pattern).
/// Se inyecta como IOptions&lt;JwtSettings&gt;.
/// Equivalente en Spring: una clase @ConfigurationProperties(prefix = "jwt") con @Validated.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Clave secreta HMAC-SHA256: mínimo 32 caracteres (256 bits). En producción, variable de entorno Jwt__Key.</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Key debe tener al menos 32 caracteres.")]
    public string Key { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpirationMinutes { get; init; } = 60;
}
