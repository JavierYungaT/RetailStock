namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Reloj del negocio: traduce un instante UTC (como se guarda en la BD) al día contable local.
/// Una venta de las 21:00 en Ecuador (02:00 UTC del día siguiente) pertenece al día local.
/// </summary>
public interface IBusinessClock
{
    DateOnly ToBusinessDate(DateTime utcDateTime);
}
