using System.ComponentModel.DataAnnotations;

namespace RetailStock.Infrastructure.Reports;

/// <summary>
/// Sección "Reports" de appsettings. Las fechas se guardan en UTC y los reportes agrupan por el día LOCAL
/// del negocio. Ecuador continental = UTC-5 sin horario de verano -> -300 minutos.
/// (Para zonas con horario de verano habría que usar TimeZoneInfo en lugar de un desfase fijo.)
/// </summary>
public sealed class ReportSettings
{
    public const string SectionName = "Reports";

    [Range(-840, 840)]
    public int UtcOffsetMinutes { get; init; }
}
