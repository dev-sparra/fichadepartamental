using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Infrastructure.Exports;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

/// <summary>
/// Entrega la plantilla oficial <b>en blanco</b> para el diligenciamiento offline. Sirve exactamente
/// el mismo archivo <c>ficha_departamental_gobernanza.xlsm</c> (con macros, validaciones, listas
/// desplegables y la hoja <c>Variables</c>), de modo que diligenciar offline sea idéntico a hacerlo
/// en la plataforma. Se descarta la generación previa de un <c>.xlsx</c> reconstruido, que divergía
/// del formato oficial (nombres de rango distintos y sin macros).
/// </summary>
public sealed class ImportTemplateService(IFichaTemplateProvider templateProvider) : IImportTemplateService
{
    public Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(templateProvider.GetTemplateBytes());
    }
}
