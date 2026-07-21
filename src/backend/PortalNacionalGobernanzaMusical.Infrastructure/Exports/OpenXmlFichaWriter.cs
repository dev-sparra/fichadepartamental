using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Exports;

/// <summary>Escribe los datos de una ficha sobre la plantilla oficial preservando el formato <c>.xlsm</c>.</summary>
public interface IFichaWorkbookWriter
{
    byte[] Write(IReadOnlyList<FichaExportSheet> sheets);
}

/// <summary>
/// Abre la plantilla oficial y sobrescribe <b>únicamente las celdas de captura</b> (campos
/// <c>Editable</c> del Blueprint), dejando intactos el proyecto VBA, las validaciones (clásicas y
/// x14), estilos, celdas protegidas, la hoja <c>Variables</c>, el modelo y las fórmulas. Las
/// columnas calculadas/fijas no se tocan: se fuerza el recálculo al abrir para que Excel actualice
/// Departamento heredado, Valor actual, % Cumplimiento, etc.
/// </summary>
public sealed class OpenXmlFichaWriter(
    IFichaTemplateProvider templateProvider,
    IFichaBlueprintProvider blueprintProvider) : IFichaWorkbookWriter
{
    public byte[] Write(IReadOnlyList<FichaExportSheet> sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);

        var blueprint = blueprintProvider.GetBlueprint();
        using var stream = templateProvider.OpenWritableTemplate();

        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            var workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException("La plantilla no contiene WorkbookPart.");

            foreach (var exportSheet in sheets)
            {
                WriteSheet(workbookPart, blueprint, exportSheet);
            }

            ForceRecalculationOnLoad(workbookPart);
            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static void WriteSheet(WorkbookPart workbookPart, FichaBlueprint blueprint, FichaExportSheet exportSheet)
    {
        var sheetBlueprint = blueprint.Sheets.FirstOrDefault(s => s.Key == exportSheet.SheetKey)
            ?? throw new InvalidOperationException($"El Blueprint no define la hoja '{exportSheet.SheetKey}'.");

        var editableFields = sheetBlueprint.Fields
            .Where(f => f.Editable)
            .ToDictionary(f => f.Key, StringComparer.Ordinal);

        var worksheetPart = GetWorksheetPart(workbookPart, sheetBlueprint.Name);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
            ?? worksheetPart.Worksheet.AppendChild(new SheetData());

        foreach (var exportRow in exportSheet.Rows)
        {
            if (exportRow.RowNumber < sheetBlueprint.DataStartRow || exportRow.RowNumber > sheetBlueprint.DataEndRow)
            {
                throw new InvalidOperationException(
                    $"La fila {exportRow.RowNumber} está fuera del rango de datos [{sheetBlueprint.DataStartRow}..{sheetBlueprint.DataEndRow}] de la hoja '{sheetBlueprint.Name}'.");
            }

            var row = GetOrCreateRow(sheetData, (uint)exportRow.RowNumber);

            foreach (var (key, value) in exportRow.Values)
            {
                if (value is null || !editableFields.TryGetValue(key, out var field))
                {
                    continue;
                }

                var cell = GetOrCreateCell(row, field.Column, (uint)exportRow.RowNumber);
                WriteCellValue(cell, field, value);
            }
        }

        worksheetPart.Worksheet.Save();
    }

    private static void WriteCellValue(Cell cell, BlueprintField field, object value)
    {
        // Se conserva el estilo existente de la celda (cell.StyleIndex no se toca).
        cell.CellFormula = null;
        cell.CellValue = null;
        cell.InlineString = null;
        cell.DataType = null;

        switch (field.Type)
        {
            case BlueprintFieldTypes.Date:
                var date = value switch
                {
                    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    DateTime dateTime => dateTime,
                    _ => throw new InvalidOperationException($"El campo de fecha '{field.Key}' recibió un valor no fecha: {value.GetType().Name}.")
                };
                cell.CellValue = new CellValue(date.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;

            case BlueprintFieldTypes.Integer:
            case BlueprintFieldTypes.Decimal:
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                cell.CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture));
                break;

            default:
                var text = value.ToString();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
                break;
        }
    }

    private static void ForceRecalculationOnLoad(WorkbookPart workbookPart)
    {
        // La cadena de cálculo quedaría obsoleta al añadir valores; se elimina para que Excel la
        // reconstruya, y se fuerza el recálculo total al abrir.
        if (workbookPart.CalculationChainPart is not null)
        {
            workbookPart.DeletePart(workbookPart.CalculationChainPart);
        }

        var workbook = workbookPart.Workbook;
        workbook.CalculationProperties ??= new CalculationProperties();
        workbook.CalculationProperties.FullCalculationOnLoad = true;
    }

    private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, string sheetName)
    {
        var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
            .FirstOrDefault(s => s.Name is not null && s.Name == sheetName)
            ?? throw new InvalidOperationException($"La plantilla no contiene la hoja '{sheetName}'.");

        return (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
    }

    private static Row GetOrCreateRow(SheetData sheetData, uint rowIndex)
    {
        var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex is not null && r.RowIndex.Value == rowIndex);
        if (row is not null)
        {
            return row;
        }

        row = new Row { RowIndex = rowIndex };
        var reference = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex is not null && r.RowIndex.Value > rowIndex);
        if (reference is null)
        {
            sheetData.Append(row);
        }
        else
        {
            sheetData.InsertBefore(row, reference);
        }

        return row;
    }

    private static Cell GetOrCreateCell(Row row, string column, uint rowIndex)
    {
        var reference = column + rowIndex.ToString(CultureInfo.InvariantCulture);
        var cell = row.Elements<Cell>()
            .FirstOrDefault(c => string.Equals(c.CellReference, reference, StringComparison.OrdinalIgnoreCase));
        if (cell is not null)
        {
            return cell;
        }

        cell = new Cell { CellReference = reference };
        var targetIndex = ColumnIndex(column);
        var referenceCell = row.Elements<Cell>()
            .FirstOrDefault(c => c.CellReference is not null && ColumnIndex(ColumnLetters(c.CellReference!)) > targetIndex);

        if (referenceCell is null)
        {
            row.Append(cell);
        }
        else
        {
            row.InsertBefore(cell, referenceCell);
        }

        return cell;
    }

    private static string ColumnLetters(string cellReference) =>
        new(cellReference.TakeWhile(char.IsLetter).ToArray());

    private static int ColumnIndex(string columnLetters)
    {
        var index = 0;
        foreach (var letter in columnLetters)
        {
            index = (index * 26) + (char.ToUpperInvariant(letter) - 'A' + 1);
        }

        return index;
    }
}
