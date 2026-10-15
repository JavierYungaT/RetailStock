using Microsoft.Extensions.Options;
using RetailStock.Application.Common.Interfaces;

namespace RetailStock.Infrastructure.Common;

/// <summary>Convierte instantes UTC al día local del negocio usando Business:UtcOffsetMinutes.</summary>
internal sealed class BusinessClock : IBusinessClock
{
    private readonly TimeSpan _offset;

    public BusinessClock(IOptions<BusinessSettings> settings)
        => _offset = TimeSpan.FromMinutes(settings.Value.UtcOffsetMinutes);

    public DateOnly ToBusinessDate(DateTime utcDateTime)
        => DateOnly.FromDateTime(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc).Add(_offset));
}
