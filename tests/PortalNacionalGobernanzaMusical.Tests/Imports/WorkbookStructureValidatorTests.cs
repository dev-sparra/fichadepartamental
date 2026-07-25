using ClosedXML.Excel;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// La estructura del libro cargado debe coincidir con el archivo oficial: 7 hojas y cada columna
/// en su posición con el encabezado esperado. Si no coincide, la importación se rechaza con
/// incidencias funcionales y sin escribir datos.
/// </summary>
public sealed class WorkbookStructureValidatorTests
{
    private const string WorkbookFileName = "ficha_departamental_gobernanza.xlsm";

    private static readonly WorkbookStructureValidator Validator = new(new FichaBlueprintProvider());

    [Fact]
    public void Validate_OfficialBlankTemplate_ShouldOnlyReportThatThereIsNoData()
    {
        using var workbook = OpenOfficialWorkbook();

        var rejections = Validator.Validate(workbook);

        // La plantilla en blanco tiene la estructura correcta: lo único reportable es que no hay datos.
        Assert.DoesNotContain(rejections, rejection => rejection.Code == ImportIssueCodes.SheetMissing);
        Assert.DoesNotContain(rejections, rejection => rejection.Code == ImportIssueCodes.HeaderMismatch);
        Assert.Single(rejections);
        Assert.Equal(ImportIssueCodes.WorkbookWithoutData, rejections[0].Code);
    }

    [Fact]
    public void Validate_OfficialWorkbookWithData_ShouldNotReportAnything()
    {
        using var workbook = OpenOfficialWorkbook();
        var identificacion = workbook.Worksheet("Identificación");
        identificacion.Cell(2, 1).Value = new DateTime(2026, 3, 15);
        identificacion.Cell(2, 2).Value = "Antioquia";

        var rejections = Validator.Validate(workbook);

        Assert.Empty(rejections);
    }

    [Fact]
    public void Validate_WhenASheetIsMissing_ShouldReportItByName()
    {
        using var workbook = OpenOfficialWorkbook();
        workbook.Worksheet("Actores").Delete();

        var rejections = Validator.Validate(workbook);

        var missing = Assert.Single(rejections, rejection => rejection.Code == ImportIssueCodes.SheetMissing);
        Assert.Equal("Actores", missing.SheetName);
        Assert.Contains("Actores", missing.Message);
    }

    [Fact]
    public void Validate_WhenAColumnHeaderChanged_ShouldReportSheetColumnAndExpectedHeader()
    {
        using var workbook = OpenOfficialWorkbook();
        // Simula un archivo manipulado: se renombra el encabezado de Actores!G (Correo electrónico).
        workbook.Worksheet("Actores").Cell(1, 7).Value = "Mail";

        var rejections = Validator.Validate(workbook);

        var mismatch = Assert.Single(rejections, rejection => rejection.Code == ImportIssueCodes.HeaderMismatch);
        Assert.Equal("Actores", mismatch.SheetName);
        Assert.Equal("G1", mismatch.CellReference);
        Assert.Contains("Correo electrónico", mismatch.Expected);
        Assert.Equal("Mail", mismatch.RawValue);
    }

    [Fact]
    public void Validate_WhenAColumnWasDeleted_ShouldReportTheMissingColumn()
    {
        using var workbook = OpenOfficialWorkbook();
        workbook.Worksheet("Identificación").Cell(1, 5).Clear();

        var rejections = Validator.Validate(workbook);

        var mismatch = Assert.Single(rejections, rejection =>
            rejection.Code == ImportIssueCodes.HeaderMismatch && rejection.SheetName == "Identificación");
        Assert.Contains("Región OCAD", mismatch.Message);
    }

    private static XLWorkbook OpenOfficialWorkbook() => new(ResolveWorkbookPath());

    private static string ResolveWorkbookPath()
    {
        var local = Path.Combine(AppContext.BaseDirectory, WorkbookFileName);
        if (File.Exists(local))
        {
            return local;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "docs", WorkbookFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"No se encontró {WorkbookFileName} para las pruebas de estructura.");
    }
}
