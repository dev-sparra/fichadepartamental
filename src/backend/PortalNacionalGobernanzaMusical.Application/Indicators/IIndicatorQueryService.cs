namespace PortalNacionalGobernanzaMusical.Application.Indicators;

public interface IIndicatorQueryService
{
    Task<IReadOnlyCollection<IndicatorRecordDto>> GetIndicatorRecordsAsync(CancellationToken cancellationToken = default);
    Task<IndicatorRecordDto?> GetIndicatorRecordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IndicatorRecordDto> UpdateIndicatorRecordAsync(Guid id, UpdateIndicatorRecordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IndicatorDetailRecordDto>> GetDetailRecordsAsync(CancellationToken cancellationToken = default);
    Task<IndicatorDetailRecordDto?> GetDetailRecordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IndicatorDetailRecordDto> UpdateDetailRecordAsync(Guid id, UpdateIndicatorDetailRecordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Materializa (crea si no existen) los 7 indicadores fijos y sus detalles para un departamento
    /// y año, y los devuelve listos para diligenciar. Idempotente.
    /// </summary>
    Task<IndicatorWorksheetDto> ProvisionWorksheetAsync(int departmentId, int year, CancellationToken cancellationToken = default);
}
