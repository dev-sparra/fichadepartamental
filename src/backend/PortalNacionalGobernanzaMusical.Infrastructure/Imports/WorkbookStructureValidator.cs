using ClosedXML.Excel;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

/// <summary>
/// Verifica que el libro cargado sea realmente la Ficha Departamental de Gobernanza oficial:
/// que existan las 7 hojas del Blueprint, que cada columna esté en su posición con el encabezado
/// esperado y que el archivo traiga información diligenciada.
/// <para>Si algo no coincide, la importación se rechaza sin escribir datos y se devuelven
/// incidencias redactadas para el usuario (nunca errores técnicos).</para>
/// </summary>
public sealed class WorkbookStructureValidator(IFichaBlueprintProvider blueprintProvider)
{
    /// <summary>Máximo de columnas reportadas por hoja para no abrumar al usuario.</summary>
    private const int MaxHeaderIssuesPerSheet = 3;

    private readonly FichaBlueprint _blueprint = blueprintProvider.GetBlueprint();

    public IReadOnlyList<ImportFileRejection> Validate(IXLWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var rejections = new List<ImportFileRejection>();
        var sheetsWithData = 0;
        var missingSheets = 0;

        foreach (var sheet in _blueprint.Sheets)
        {
            if (!workbook.Worksheets.TryGetWorksheet(sheet.Name, out var worksheet))
            {
                missingSheets++;
                rejections.Add(new ImportFileRejection(
                    ImportIssueCodes.SheetMissing,
                    $"El archivo no contiene la hoja \"{sheet.Name}\", que es obligatoria en la Ficha Departamental de Gobernanza.",
                    $"Una hoja llamada exactamente \"{sheet.Name}\".",
                    $"Utilice el archivo oficial {ImportFileRules.OfficialFileName} sin eliminar ni renombrar hojas.",
                    null,
                    sheet.Name));

                continue;
            }

            rejections.AddRange(ValidateHeaders(sheet, worksheet));

            if (HasDiligencedData(sheet, worksheet))
            {
                sheetsWithData++;
            }
        }

        // Un archivo con la estructura correcta pero en blanco no debe generar una importación
        // vacía: se avisa con un mensaje claro en lugar de crear una ficha sin datos.
        if (missingSheets == 0 && rejections.Count == 0 && sheetsWithData == 0)
        {
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.WorkbookWithoutData,
                "El archivo corresponde a la plantilla oficial, pero no tiene información diligenciada.",
                "Al menos la hoja \"Identificación\" diligenciada con la fecha de levantamiento y el departamento.",
                "Diligencie la ficha en el archivo oficial, guárdelo y vuelva a cargarlo.",
                null));
        }

        return rejections;
    }

    private static IEnumerable<ImportFileRejection> ValidateHeaders(BlueprintSheet sheet, IXLWorksheet worksheet)
    {
        var reported = 0;

        foreach (var field in sheet.Fields)
        {
            if (reported == MaxHeaderIssuesPerSheet)
            {
                yield break;
            }

            var expected = BlueprintFieldLocator.ExpectedHeader(field);
            var found = BlueprintFieldLocator.NormalizeHeader(
                worksheet.Cell(sheet.HeaderRow, field.ColumnIndex).GetFormattedString());

            if (string.Equals(expected, found, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            reported++;

            var message = string.IsNullOrWhiteSpace(found)
                ? $"En la hoja \"{sheet.Name}\" falta la columna \"{field.Label}\" (columna {field.Column})."
                : $"En la hoja \"{sheet.Name}\" la columna {field.Column} debería ser \"{expected}\" y contiene \"{found}\".";

            yield return new ImportFileRejection(
                ImportIssueCodes.HeaderMismatch,
                message,
                $"El encabezado \"{expected}\" en la columna {field.Column} de la hoja \"{sheet.Name}\".",
                $"No modifique, mueva ni elimine columnas del archivo oficial {ImportFileRules.OfficialFileName}. Descargue la plantilla y vuelva a diligenciarla.",
                string.IsNullOrWhiteSpace(found) ? null : found,
                sheet.Name,
                sheet.HeaderRow,
                field.Column + sheet.HeaderRow);
        }
    }

    /// <summary>
    /// Indica si la hoja trae información capturada por el usuario. Solo se revisan las columnas
    /// editables: las de catálogo fijo (Acción, Nombre Indicador, Meta) y las calculadas vienen
    /// diligenciadas incluso en la plantilla en blanco.
    /// </summary>
    private static bool HasDiligencedData(BlueprintSheet sheet, IXLWorksheet worksheet)
    {
        var editableColumns = sheet.Fields
            .Where(field => field.Editable
                && field.Type is not BlueprintFieldTypes.Fixed
                && field.Type is not BlueprintFieldTypes.Calculated)
            .Select(field => field.ColumnIndex)
            .ToArray();

        if (editableColumns.Length == 0)
        {
            return false;
        }

        for (var row = sheet.DataStartRow; row <= sheet.DataEndRow; row++)
        {
            foreach (var column in editableColumns)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cell(row, column).GetFormattedString()))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
