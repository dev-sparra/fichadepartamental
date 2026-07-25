namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>Incidencia tal como quedó almacenada (datos crudos de la validación).</summary>
public sealed record ImportIssueSource(
    Guid Id,
    string Severity,
    string SheetName,
    int? RowNumber,
    string? CellReference,
    string ErrorCode,
    string Message,
    string? RawValue,
    string? ContextJson);

/// <summary>
/// Convierte una incidencia técnica en un mensaje funcional para el usuario: ubica hoja, fila,
/// columna y nombre del campo (tal como aparece en la Ficha Departamental del portal), indica el
/// valor recibido, el valor esperado y cómo corregirlo.
/// </summary>
public interface IImportIssueNarrator
{
    ImportValidationIssueDto Narrate(ImportIssueSource issue);
}
