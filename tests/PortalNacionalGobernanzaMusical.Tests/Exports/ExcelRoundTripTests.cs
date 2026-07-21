using ClosedXML.Excel;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Infrastructure.Exports;

namespace PortalNacionalGobernanzaMusical.Tests.Exports;

/// <summary>
/// Prueba E2E del round-trip Excel↔Web: verifica que (1) la librería de importación (ClosedXML)
/// puede abrir el `.xlsm` oficial —con su modelo Power Query, macros y validaciones— y (2) que un
/// libro exportado se relee con los mismos valores en las mismas celdas que espera la importación,
/// incluida la selección múltiple con separador ", " y las fechas reales.
/// </summary>
public sealed class ExcelRoundTripTests
{
    [Fact]
    public void OfficialTemplate_ShouldBeOpenableByImportReader()
    {
        var bytes = new FichaTemplateProvider().GetTemplateBytes();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheetNames = workbook.Worksheets.Select(w => w.Name).ToList();

        Assert.Contains("Identificación", sheetNames);
        Assert.Contains("Actores", sheetNames);
        Assert.Contains("Indicadores", sheetNames);
    }

    [Fact]
    public void ExportedFicha_ShouldRoundTripThroughImportReaderWithSameValues()
    {
        var writer = new OpenXmlFichaWriter(new FichaTemplateProvider(), new FichaBlueprintProvider());

        var bytes = writer.Write(new List<FichaExportSheet>
        {
            new("identificacion",
            [
                new FichaExportRow(2, new Dictionary<string, object?>
                {
                    ["fechaLevantamiento"] = new DateOnly(2026, 3, 15),
                    ["departmentId"] = "Antioquia",
                    ["municipalityId"] = "Medellín",
                    ["responsableRegistro"] = "Ana Gestora",
                    ["regionOcadOptionId"] = "Eje Cafetero",
                    ["informationSourceIds"] = "Ente territorial, CODEMUS",
                    ["observaciones"] = "Observación de prueba"
                })
            ]),
            new("actores",
            [
                new FichaExportRow(2, new Dictionary<string, object?>
                {
                    ["nombreAgente"] = "Corporación Musical",
                    ["ecosystemRoleIds"] = "Alcaldías, Gobernaciones",
                    ["territorialLevelOptionIds"] = "Municipal, Departamental",
                    ["numeroContacto"] = "3001234567",
                    ["correoElectronico"] = "contacto@corp.org"
                })
            ])
        });

        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        var identificacion = workbook.Worksheet("Identificación");
        Assert.Equal(new DateTime(2026, 3, 15), identificacion.Cell(2, 1).GetDateTime()); // A: fecha real
        Assert.Equal("Antioquia", identificacion.Cell(2, 2).GetString());                 // B: departamento
        Assert.Equal("Medellín", identificacion.Cell(2, 3).GetString());                  // C: ciudad
        Assert.Equal("Ana Gestora", identificacion.Cell(2, 4).GetString());               // D
        Assert.Equal("Eje Cafetero", identificacion.Cell(2, 5).GetString());              // E
        Assert.Equal("Ente territorial, CODEMUS", identificacion.Cell(2, 6).GetString()); // F: multi ", "

        var actores = workbook.Worksheet("Actores");
        Assert.Equal("Corporación Musical", actores.Cell(2, 2).GetString());              // B
        Assert.Equal("Alcaldías, Gobernaciones", actores.Cell(2, 4).GetString());         // D: rol multi
        Assert.Equal("Municipal, Departamental", actores.Cell(2, 5).GetString());         // E: nivel multi
        Assert.Equal("3001234567", actores.Cell(2, 6).GetString());                       // F
        Assert.Equal("contacto@corp.org", actores.Cell(2, 7).GetString());                // G
    }
}
