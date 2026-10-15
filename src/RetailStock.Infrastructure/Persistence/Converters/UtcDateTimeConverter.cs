using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetailStock.Infrastructure.Persistence.Converters;

/// <summary>
/// La aplicación guarda todas las fechas en UTC, pero datetime2 no almacena la zona horaria:
/// al leer, EF devuelve DateTime con Kind = Unspecified y el JSON sale sin la "Z" final
/// ("2026-10-15T20:47:22" en vez de "2026-10-15T20:47:22Z"), y el navegador la tomaría como hora local.
/// Este converter marca como UTC todo DateTime leído de la BD (no cambia el esquema).
/// Equivalente en JPA: un AttributeConverter o hibernate.jdbc.time_zone=UTC.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            toDb => toDb.Kind == DateTimeKind.Utc ? toDb : toDb.ToUniversalTime(),
            fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
    {
    }
}
