using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortalNacionalGobernanzaMusical.Infrastructure.Exports;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Verifica que la plantilla de importación descargable sea exactamente el archivo oficial
/// <c>.xlsm</c> en blanco (no un .xlsx reconstruido), garantizando el round-trip offline↔web.
/// </summary>
public sealed class ImportTemplateServiceTests
{
    [Fact]
    public async Task GenerateTemplate_ShouldReturnOfficialTemplateBytes()
    {
        var provider = new FichaTemplateProvider();
        var service = new ImportTemplateService(provider);

        var templateBytes = await service.GenerateTemplateAsync();

        Assert.Equal(provider.GetTemplateBytes(), templateBytes);
    }

    [Fact]
    public async Task GenerateTemplate_ShouldBeMacroEnabledWithCatalogsAndBlankData()
    {
        var service = new ImportTemplateService(new FichaTemplateProvider());

        var templateBytes = await service.GenerateTemplateAsync();

        using var stream = new MemoryStream(templateBytes);
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart!;

        // Macros preservadas (es .xlsm, no .xlsx).
        Assert.NotNull(workbookPart.VbaProjectPart);

        // Hoja de catálogos y rangos con nombre presentes.
        var sheetNames = workbookPart.Workbook.Sheets!.Elements<Sheet>().Select(s => s.Name!.Value).ToHashSet();
        Assert.Contains("Variables", sheetNames);
        Assert.Contains("Identificación", sheetNames);

        var definedNames = workbookPart.Workbook.DefinedNames!.Elements<DefinedName>().Select(d => d.Name!.Value).ToHashSet();
        Assert.Contains("Departamentos", definedNames);

        // En blanco: la primera fila de datos de Identificación no trae valores.
        var identificacion = (WorksheetPart)workbookPart.GetPartById(
            workbookPart.Workbook.Sheets!.Elements<Sheet>().First(s => s.Name == "Identificación").Id!);

        var b2 = identificacion.Worksheet.GetFirstChild<SheetData>()!
            .Elements<Row>()
            .SelectMany(r => r.Elements<Cell>())
            .FirstOrDefault(c => c.CellReference == "B2");

        Assert.True(b2 is null || (b2.CellValue is null && b2.InlineString is null),
            "La plantilla descargable debe estar en blanco (Identificación!B2 sin valor).");
    }
}
