namespace PortalNacionalGobernanzaMusical.Application.Exports;

public interface IExcelExportService
{
    Task<byte[]> ExportFichaToExcelAsync(Guid fichaId, CancellationToken cancellationToken = default);
}
