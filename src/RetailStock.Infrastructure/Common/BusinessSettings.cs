using System.ComponentModel.DataAnnotations;

namespace RetailStock.Infrastructure.Common;

/// <summary>
/// Sección "Business" de appsettings: datos generales del negocio.
/// Las fechas se guardan en UTC; reportes y facturas SAP usan el día LOCAL del negocio.
/// Ecuador continental = UTC-5 sin horario de verano -> -300 minutos.
/// (Para zonas con horario de verano habría que usar TimeZoneInfo en lugar de un desfase fijo.)
/// </summary>
public sealed class BusinessSettings
{
    public const string SectionName = "Business";

    [Range(-840, 840)]
    public int UtcOffsetMinutes { get; init; }
}
