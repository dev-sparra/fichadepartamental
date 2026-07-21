namespace PortalNacionalGobernanzaMusical.Application.Imports;

public interface IWorkbookImportService
{
    Task<ImportWorkbookResult> ImportAsync(ImportWorkbookCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un lote de importación y todos sus registros asociados (staging rows,
    /// validation issues). No revierte los datos persistidos en fichas/indicadores.
    /// Solo disponible para administradores.
    /// </summary>
    Task DeleteBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ImportBatchSummaryDto>> GetBatchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ImportValidationIssueDto>> GetIssuesAsync(Guid importBatchId, CancellationToken cancellationToken = default);
}
