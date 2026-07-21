using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Infrastructure.Exports;

namespace PortalNacionalGobernanzaMusical.Tests.Exports;

/// <summary>
/// Verifica que la exportación sobre la plantilla oficial produzca un <c>.xlsm</c> fiel:
/// preserva macros (VBA), validaciones, rangos con nombre, estilos y fórmulas, y escribe
/// correctamente solo las celdas de captura.
/// </summary>
public sealed class OpenXmlFichaWriterTests
{
    private static byte[] Export()
    {
        var writer = new OpenXmlFichaWriter(new FichaTemplateProvider(), new FichaBlueprintProvider());

        var sheets = new List<FichaExportSheet>
        {
            new("identificacion",
            [
                new FichaExportRow(2, new Dictionary<string, object?>
                {
                    ["fechaLevantamiento"] = new DateOnly(2026, 3, 15),
                    ["departmentId"] = "Antioquia",
                    ["municipalityId"] = "Medellín",
                    ["responsableRegistro"] = "Gestor de prueba",
                    ["regionOcadOptionId"] = "Eje Cafetero",
                    ["informationSourceIds"] = "Ente territorial, CODEMUS",
                    ["observaciones"] = "Observación de prueba"
                })
            ]),
            // Hoja del Líder: prueba que el escritor es genérico y respeta columnas fijas/calculadas.
            new("indicadores",
            [
                new FichaExportRow(3, new Dictionary<string, object?>
                {
                    ["departments"] = "Antioquia",
                    ["avanceEnero"] = 3m,
                    ["source"] = "Fuente de prueba",
                    ["year"] = 2026
                })
            ])
        };

        return writer.Write(sheets);
    }

    [Fact]
    public void Export_ShouldPreserveVbaMacros()
    {
        using var document = OpenExported();
        Assert.NotNull(document.WorkbookPart!.VbaProjectPart);
    }

    [Fact]
    public void Export_ShouldPreserveNamedRangesAndSheets()
    {
        using var document = OpenExported();
        var workbook = document.WorkbookPart!.Workbook;

        var definedNames = workbook.DefinedNames!.Elements<DefinedName>().Select(d => d.Name!.Value).ToHashSet();
        Assert.Contains("Departamentos", definedNames);
        Assert.Contains("Multi_Fuente", definedNames);

        var sheetNames = workbook.Sheets!.Elements<Sheet>().Select(s => s.Name!.Value).ToList();
        Assert.Equal(new[] { "Identificación", "Diagnóstico ecosistema", "Oportunidades de cambio", "Ejes PNMC", "Actores", "Indicadores", "Detalle Indicadores", "Variables" }, sheetNames);
    }

    [Fact]
    public void Export_ShouldPreserveDataValidations()
    {
        using var document = OpenExported();
        var worksheet = WorksheetByName(document.WorkbookPart!, "Identificación").Worksheet;

        var validationCount = worksheet.Elements<DataValidations>()
            .SelectMany(dv => dv.Elements<DataValidation>())
            .Count();

        Assert.True(validationCount >= 3, $"Se esperaban validaciones clásicas en Identificación, hubo {validationCount}.");
    }

    [Fact]
    public void Export_ShouldWriteTextAndMultiSelectValues()
    {
        using var document = OpenExported();
        var worksheetPart = WorksheetByName(document.WorkbookPart!, "Identificación");

        Assert.Equal("Antioquia", InlineText(FindCell(worksheetPart, "B2")));
        Assert.Equal("Medellín", InlineText(FindCell(worksheetPart, "C2")));
        Assert.Equal("Ente territorial, CODEMUS", InlineText(FindCell(worksheetPart, "F2")));
    }

    [Fact]
    public void Export_ShouldWriteDateAsSerialPreservingStyle()
    {
        using var document = OpenExported();
        var cell = FindCell(WorksheetByName(document.WorkbookPart!, "Identificación"), "A2");

        Assert.NotNull(cell);
        Assert.Null(cell!.DataType); // número (fecha serial), no texto
        Assert.Equal(new DateOnly(2026, 3, 15).ToDateTime(TimeOnly.MinValue).ToOADate(),
            double.Parse(cell.CellValue!.InnerText, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(3u, cell.StyleIndex!.Value); // estilo dd/mm/yyyy preservado
    }

    [Fact]
    public void Export_ShouldNotTouchFixedOrCalculatedColumns()
    {
        using var document = OpenExported();
        var indicadores = WorksheetByName(document.WorkbookPart!, "Indicadores");

        // Columna escrita (avance de enero) = número.
        Assert.Equal("3", FindCell(indicadores, "E3")!.CellValue!.InnerText);

        // Columna fija (Nombre Indicador) intacta = shared string del catálogo.
        Assert.Equal(CellValues.SharedString, FindCell(indicadores, "C3")!.DataType!.Value);

        // Columna calculada (Valor actual) conserva su fórmula.
        Assert.NotNull(FindCell(indicadores, "AC3")!.CellFormula);
    }

    [Fact]
    public void Export_ShouldForceRecalculationAndDropStaleCalcChain()
    {
        using var document = OpenExported();

        Assert.Null(document.WorkbookPart!.CalculationChainPart);
        Assert.True(document.WorkbookPart.Workbook.CalculationProperties!.FullCalculationOnLoad!.Value);
    }

    // ------------------------------------------------------------------ helpers

    private static SpreadsheetDocument OpenExported()
    {
        var stream = new MemoryStream(Export());
        return SpreadsheetDocument.Open(stream, false);
    }

    private static WorksheetPart WorksheetByName(WorkbookPart workbookPart, string sheetName)
    {
        var sheet = workbookPart.Workbook.Sheets!.Elements<Sheet>().First(s => s.Name == sheetName);
        return (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
    }

    private static Cell? FindCell(WorksheetPart worksheetPart, string reference)
    {
        return worksheetPart.Worksheet.GetFirstChild<SheetData>()!
            .Elements<Row>()
            .SelectMany(r => r.Elements<Cell>())
            .FirstOrDefault(c => c.CellReference == reference);
    }

    private static string? InlineText(Cell? cell) => cell?.InlineString?.Text?.Text;
}
