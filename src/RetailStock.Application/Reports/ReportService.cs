namespace RetailStock.Application.Reports;

/// <summary>
/// Servicio delgado: los reportes no tienen reglas de negocio más allá de validar parámetros
/// (lo hacen las DataAnnotations del query). Se mantiene la capa para que el controlador no dependa
/// del repositorio y para tener un lugar donde agregar caché o permisos por bodega en el futuro.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository) => _reportRepository = reportRepository;

    // From/To no son null aquí: [Required] ya los validó en el controlador
    public Task<IReadOnlyList<SalesByDayResponse>> GetSalesByDayAsync(SalesByDayQuery query, CancellationToken cancellationToken = default)
        => _reportRepository.GetSalesByDayAsync(query.From!.Value, query.To!.Value, cancellationToken);

    public Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(LowStockQuery query, CancellationToken cancellationToken = default)
        => _reportRepository.GetLowStockAsync(query.Threshold, cancellationToken);

    public Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(TopProductsQuery query, CancellationToken cancellationToken = default)
        => _reportRepository.GetTopProductsAsync(query.From!.Value, query.To!.Value, query.Top, cancellationToken);
}
