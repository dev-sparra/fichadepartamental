using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.Tests.Governance;

/// <summary>
/// Verifica que el <see cref="FichaBlueprintProvider"/> (SSOT en código) siga fiel al archivo
/// oficial <c>ficha_departamental_gobernanza.xlsm</c>. Si el Excel cambia (hojas, columnas,
/// rangos con nombre, tablas) estas pruebas fallan y obligan a actualizar el Blueprint.
/// </summary>
public sealed class FichaBlueprintParityTests
{
    private static readonly FichaBlueprint Blueprint = new FichaBlueprintProvider().GetBlueprint();

    private const string WorkbookFileName = "ficha_departamental_gobernanza.xlsm";

    [Fact]
    public void Workbook_ShouldBeAvailableForVerification()
    {
        Assert.True(File.Exists(ResolveWorkbookPath()),
            $"No se encontró {WorkbookFileName} junto al ensamblado de pruebas ni en la carpeta docs/.");
    }

    [Fact]
    public void SheetNames_ShouldMatchBlueprintInOrder()
    {
        using var doc = Open();
        var workbookSheets = SheetNames(doc);
        var blueprintSheets = Blueprint.Sheets.Select(s => s.Name).ToList();

        // Las 7 hojas del Blueprint deben aparecer, en el mismo orden, al inicio del libro.
        Assert.Equal(blueprintSheets, workbookSheets.Take(blueprintSheets.Count).ToList());

        // La hoja de catálogos existe y NO forma parte del Blueprint (es interna).
        Assert.Contains("Variables", workbookSheets);
        Assert.DoesNotContain("Variables", blueprintSheets);
    }

    [Fact]
    public void EveryReferencedNamedRange_ShouldExistInWorkbook()
    {
        using var doc = Open();
        var defined = DefinedNames(doc);

        foreach (var range in ReferencedNamedRanges())
        {
            Assert.True(defined.Contains(range),
                $"El rango con nombre '{range}' referenciado por el Blueprint no existe en el Excel.");
        }
    }

    [Fact]
    public void EveryMultiRange_ShouldExistInWorkbook()
    {
        using var doc = Open();
        var defined = DefinedNames(doc);

        var multiRanges = Blueprint.Sheets
            .SelectMany(s => s.Fields)
            .Where(f => f.MultiRange is not null)
            .Select(f => f.MultiRange!)
            .Distinct();

        foreach (var range in multiRanges)
        {
            Assert.True(defined.Contains(range),
                $"El rango de selección múltiple '{range}' no existe en el Excel.");
        }
    }

    [Fact]
    public void TableColumnCounts_ShouldMatchFieldCounts()
    {
        using var doc = Open();
        var wb = doc.WorkbookPart!;

        foreach (var sheet in Blueprint.Sheets.Where(s => s.Table is not null))
        {
            var table = FindTable(wb, sheet.Name, sheet.Table!);
            Assert.NotNull(table);

            var columnCount = table!.TableColumns!.Elements<TableColumn>().Count();
            Assert.True(sheet.Fields.Count == columnCount,
                $"Hoja '{sheet.Name}': el Blueprint define {sheet.Fields.Count} campos pero la tabla tiene {columnCount} columnas.");
        }
    }

    [Fact]
    public void GestorSheetHeaders_ShouldMatchBlueprintLabels()
    {
        // Hojas del Gestor con encabezados estables (Indicadores usa nombres duplicados por mes y se excluye).
        var sheetKeys = new[] { "identificacion", "diagnostico", "oportunidades", "ejes-pnmc", "actores" };

        using var doc = Open();
        var wb = doc.WorkbookPart!;

        foreach (var sheet in Blueprint.Sheets.Where(s => sheetKeys.Contains(s.Key)))
        {
            var table = FindTable(wb, sheet.Name, sheet.Table!);
            Assert.NotNull(table);

            var headers = table!.TableColumns!.Elements<TableColumn>()
                .Select(c => c.Name?.Value ?? string.Empty)
                .ToList();

            foreach (var field in sheet.Fields)
            {
                var tableIndex = field.ColumnIndex - 1; // las tablas inician en la columna A
                Assert.True(tableIndex < headers.Count, $"Hoja '{sheet.Name}': falta la columna {field.Column}.");
                Assert.Equal(field.Label, Normalize(headers[tableIndex]));
            }
        }
    }

    [Fact]
    public void MultiSelectSeparator_ShouldBeCommaSpaceEverywhere()
    {
        Assert.Equal(", ", Blueprint.MultiSelectSeparator);

        var multiFields = Blueprint.Sheets.SelectMany(s => s.Fields).Where(f => f.MultiSelect);
        Assert.All(multiFields, f => Assert.Equal(", ", f.MultiSeparator));
    }

    // ------------------------------------------------------------------ helpers

    private static IEnumerable<string> ReferencedNamedRanges()
    {
        return Blueprint.Sheets
            .SelectMany(s => s.Fields)
            .Where(f => f.ExcelRange is not null)
            .Select(f => f.ExcelRange!)
            .Distinct();
    }

    private static SpreadsheetDocument Open() => SpreadsheetDocument.Open(ResolveWorkbookPath(), false);

    private static List<string> SheetNames(SpreadsheetDocument doc)
    {
        return doc.WorkbookPart!.Workbook.Sheets!
            .Elements<Sheet>()
            .Select(s => s.Name!.Value!)
            .ToList();
    }

    private static HashSet<string> DefinedNames(SpreadsheetDocument doc)
    {
        var names = doc.WorkbookPart!.Workbook.DefinedNames?
            .Elements<DefinedName>()
            .Select(d => d.Name!.Value!)
            ?? Enumerable.Empty<string>();

        return new HashSet<string>(names, StringComparer.Ordinal);
    }

    private static Table? FindTable(WorkbookPart wb, string sheetName, string tableName)
    {
        var sheet = wb.Workbook.Sheets!.Elements<Sheet>().First(s => s.Name == sheetName);
        var wsPart = (WorksheetPart)wb.GetPartById(sheet.Id!.Value!);

        return wsPart.TableDefinitionParts
            .Select(p => p.Table)
            .FirstOrDefault(t => string.Equals(t.DisplayName?.Value ?? t.Name?.Value, tableName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reduce un encabezado del Excel a su etiqueta base: corta el sufijo "◉ Selección múltiple"
    /// (almacenado como salto de línea escapado <c>_x000a_</c>) y elimina espacios de ancho cero.
    /// </summary>
    private static string Normalize(string header)
    {
        var cut = header.IndexOf("_x000a_", StringComparison.OrdinalIgnoreCase);
        if (cut >= 0)
        {
            header = header[..cut];
        }

        var newLine = header.IndexOf('\n');
        if (newLine >= 0)
        {
            header = header[..newLine];
        }

        return header
            .Replace("​", string.Empty)
            .Replace("﻿", string.Empty)
            .Replace("\r", string.Empty)
            .Trim();
    }

    private static string ResolveWorkbookPath()
    {
        var local = Path.Combine(AppContext.BaseDirectory, WorkbookFileName);
        if (File.Exists(local))
        {
            return local;
        }

        // Respaldo: buscar docs/ subiendo por el árbol de directorios.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "docs", WorkbookFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return local;
    }
}
