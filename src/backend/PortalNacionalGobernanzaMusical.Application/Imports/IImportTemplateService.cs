namespace PortalNacionalGobernanzaMusical.Application.Imports;

public interface IImportTemplateService
{
    Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default);
}
