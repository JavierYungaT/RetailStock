using System.ComponentModel.DataAnnotations;

namespace RetailStock.Infrastructure.Sap;

/// <summary>
/// Sección "Sap" de appsettings (Options pattern). La contraseña NO va en appsettings.json:
/// en producción se pasa como variable de entorno Sap__Password o desde un secret manager.
/// </summary>
public sealed class SapSettings
{
    public const string SectionName = "Sap";

    /// <summary>true = SapServiceLayerMock (desarrollo, sin servidor SAP); false = HttpClient real.</summary>
    public bool UseMock { get; init; } = true;

    /// <summary>Ej: https://sap-server:50000/b1s/v1/ (con "/" final para que las rutas relativas se concatenen bien).</summary>
    public string BaseUrl { get; init; } = string.Empty;

    public string CompanyDB { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 60;

    public SapBackgroundSyncSettings BackgroundSync { get; init; } = new();

    /// <summary>Con el cliente real, las credenciales son obligatorias.</summary>
    public bool HasRealConnectionSettings()
        => Uri.TryCreate(BaseUrl, UriKind.Absolute, out _)
           && !string.IsNullOrWhiteSpace(CompanyDB)
           && !string.IsNullOrWhiteSpace(UserName)
           && !string.IsNullOrWhiteSpace(Password);
}

public sealed class SapBackgroundSyncSettings
{
    public bool Enabled { get; init; }

    [Range(1, 1440)]
    public int IntervalMinutes { get; init; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 50;
}
