using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PortalNacionalGobernanzaMusical.Tests.Governance;

/// <summary>
/// Verifica que el seed maestro de catálogos (<c>database/seed/001_master_catalogs.sql</c>)
/// reproduzca fielmente la hoja <c>Variables</c> del Excel oficial. Garantiza el principio
/// "todas las opciones del formulario salen de la BD manteniendo la lógica del Excel":
/// si el seed o el Excel cambian y dejan de coincidir, estas pruebas fallan.
/// </summary>
public sealed class CatalogSeedParityTests
{
    private const string WorkbookFileName = "ficha_departamental_gobernanza.xlsm";

    private static readonly Lazy<ExcelData> Excel = new(BuildExcelData);
    private static readonly Lazy<string> Seed = new(() => File.ReadAllText(ResolveSeedPath()));

    // Catálogos simples: rango con nombre del Excel  <->  tabla del seed.
    public static IEnumerable<object[]> SimpleCatalogs() =>
    [
        ["RegionOCAD", "catalog_region_ocad"],
        ["EstadoComite", "catalog_committee_statuses"],
        ["EstadoPlan", "catalog_plan_statuses"],
        ["NivelAMB", "catalog_priority_levels"],
        ["EjePNMC", "catalog_pnmc_axes"],
        ["Enfoques", "catalog_approach_options"],
        ["Cronograma", "catalog_schedule_options"],
        ["EstadoPropuesta", "catalog_proposal_statuses"],
        ["TiposAgente", "catalog_agent_types"],
        ["NivelTerritorial", "catalog_territorial_levels"],
        ["FuenteInfo", "catalog_information_sources"],
        ["Meses", "catalog_months"]
    ];

    [Theory]
    [MemberData(nameof(SimpleCatalogs))]
    public void SimpleCatalog_SeedShouldMatchExcelExactly(string excelRange, string seedTable)
    {
        var excelValues = ReadNamedRange(excelRange);
        var block = SeedBlock(seedTable);

        Assert.True(excelValues.Count == CountTuples(block),
            $"{seedTable}: el Excel ({excelRange}) tiene {excelValues.Count} valores y el seed {CountTuples(block)}.");

        foreach (var value in excelValues)
        {
            AssertSeedContains(block, seedTable, excelRange, value);
        }
    }

    [Fact]
    public void Years_SeedShouldMatchExcelExactly()
    {
        var excelYears = ReadNamedRange("Años");
        var block = SeedBlock("catalog_years");

        Assert.Equal(excelYears.Count, CountTuples(block));

        foreach (var year in excelYears)
        {
            // catalog_years: (id, value, is_active) -> el año es el 2.º campo.
            Assert.True(Regex.IsMatch(block, $@"\(\s*\d+\s*,\s*{Regex.Escape(year)}\s*,"),
                $"El año '{year}' del rango Años no está en catalog_years.");
        }
    }

    [Fact]
    public void Departments_SeedShouldMatchExcelExactly()
    {
        var excelDepartments = ReadNamedRange("Departamentos");
        var block = SeedBlock("catalog_departments");

        Assert.Equal(33, excelDepartments.Count);
        Assert.Equal(excelDepartments.Count, CountTuples(block));

        foreach (var department in excelDepartments)
        {
            AssertSeedContains(block, "catalog_departments", "Departamentos", department);
        }
    }

    [Fact]
    public void Municipalities_TotalShouldMatchExcelSumOfCiuRanges()
    {
        var ciuRangeNames = Excel.Value.DefinedNames.Keys
            .Where(name => name.StartsWith("Ciu_", StringComparison.Ordinal));

        var excelTotal = ciuRangeNames.Sum(name => ReadNamedRange(name).Count);
        var block = SeedBlock("catalog_municipalities");

        Assert.True(excelTotal > 1000, $"Se esperaban >1000 municipios en el Excel, se leyeron {excelTotal}.");
        Assert.Equal(excelTotal, CountTuples(block));

        // Muestreo de capitales de distinta longitud de lista.
        foreach (var sample in new[] { "Leticia", "Medellín", "Bogotá D.C.", "Mitú" })
        {
            AssertSeedContains(block, "catalog_municipalities", "Ciu_*", sample);
        }
    }

    [Fact]
    public void Components_PerAxis_SeedShouldMatchExcel()
    {
        var expected = new[]
        {
            ReadNamedRange("Comp_Eje_1").Count,
            ReadNamedRange("Comp_Eje_2").Count,
            ReadNamedRange("Comp_Eje_3").Count
        };

        var actual = CountByForeignKey("catalog_pnmc_components");
        Assert.Equal(expected[0], actual.GetValueOrDefault(1));
        Assert.Equal(expected[1], actual.GetValueOrDefault(2));
        Assert.Equal(expected[2], actual.GetValueOrDefault(3));

        var block = SeedBlock("catalog_pnmc_components");
        foreach (var range in new[] { "Comp_Eje_1", "Comp_Eje_2", "Comp_Eje_3" })
        {
            foreach (var value in ReadNamedRange(range))
            {
                AssertSeedContains(block, "catalog_pnmc_components", range, value);
            }
        }
    }

    [Fact]
    public void Roles_PerAgentType_SeedShouldMatchExcel()
    {
        var expected = new[]
        {
            ReadNamedRange("Acto_Inst_Int").Count,
            ReadNamedRange("Acto_Inst_Ext").Count,
            ReadNamedRange("Acto_Sect").Count,
            ReadNamedRange("Acto_Comu").Count
        };

        var actual = CountByForeignKey("catalog_ecosystem_roles");
        Assert.Equal(expected[0], actual.GetValueOrDefault(1));
        Assert.Equal(expected[1], actual.GetValueOrDefault(2));
        Assert.Equal(expected[2], actual.GetValueOrDefault(3));
        Assert.Equal(expected[3], actual.GetValueOrDefault(4));

        var block = SeedBlock("catalog_ecosystem_roles");
        foreach (var range in new[] { "Acto_Inst_Int", "Acto_Inst_Ext", "Acto_Sect", "Acto_Comu" })
        {
            foreach (var value in ReadNamedRange(range))
            {
                AssertSeedContains(block, "catalog_ecosystem_roles", range, value);
            }
        }
    }

    // ------------------------------------------------------------------ seed helpers

    private static string SeedBlock(string table)
    {
        var marker = $"INSERT INTO {table} (";
        var start = Seed.Value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0, $"No se encontró el bloque INSERT de '{table}' en el seed.");

        var end = Seed.Value.IndexOf("ON DUPLICATE", start, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? Seed.Value[start..] : Seed.Value[start..end];
    }

    private static int CountTuples(string block) =>
        Regex.Matches(block, @"^\s*\(\s*\d+", RegexOptions.Multiline).Count;

    private static Dictionary<int, int> CountByForeignKey(string table)
    {
        var counts = new Dictionary<int, int>();
        foreach (Match match in Regex.Matches(SeedBlock(table), @"^\s*\(\s*\d+\s*,\s*(\d+)\s*,", RegexOptions.Multiline))
        {
            var foreignKey = int.Parse(match.Groups[1].Value);
            counts[foreignKey] = counts.GetValueOrDefault(foreignKey) + 1;
        }

        return counts;
    }

    private static void AssertSeedContains(string block, string table, string range, string value)
    {
        var needle = "'" + value.Replace("'", "''") + "'";
        Assert.True(block.Contains(needle, StringComparison.Ordinal),
            $"El valor '{value}' del rango '{range}' no aparece en el seed de '{table}'.");
    }

    // ------------------------------------------------------------------ excel helpers

    private static List<string> ReadNamedRange(string name)
    {
        Assert.True(Excel.Value.DefinedNames.TryGetValue(name, out var refText),
            $"El Excel no define el rango con nombre '{name}'.");

        var (column, firstRow, lastRow) = ParseVerticalReference(refText!);
        var values = new List<string>();
        for (var row = firstRow; row <= lastRow; row++)
        {
            if (Excel.Value.VariablesCells.TryGetValue($"{column}{row}", out var value) && !string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static (string Column, int FirstRow, int LastRow) ParseVerticalReference(string refText)
    {
        var rangePart = refText[(refText.IndexOf('!') + 1)..].Replace("$", string.Empty);
        var bounds = rangePart.Split(':');
        var (column, firstRow) = SplitCell(bounds[0]);
        if (bounds.Length == 1)
        {
            return (column, firstRow, firstRow);
        }

        var (_, lastRow) = SplitCell(bounds[1]);
        return (column, firstRow, lastRow);
    }

    private static (string Column, int Row) SplitCell(string cell)
    {
        var match = Regex.Match(cell, @"^([A-Z]+)(\d+)$");
        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
    }

    private static ExcelData BuildExcelData()
    {
        using var document = SpreadsheetDocument.Open(ResolveWorkbookPath(), false);
        var workbookPart = document.WorkbookPart!;

        var sharedStrings = workbookPart.SharedStringTablePart!.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(item => item.InnerText)
            .ToArray();

        var definedNames = workbookPart.Workbook.DefinedNames?
            .Elements<DefinedName>()
            .Where(d => d.Name?.Value is not null && !string.IsNullOrEmpty(d.Text))
            .GroupBy(d => d.Name!.Value!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Text!, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        var variablesSheet = workbookPart.Workbook.Sheets!.Elements<Sheet>().First(s => s.Name == "Variables");
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(variablesSheet.Id!.Value!);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

        var cells = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cell in sheetData.Elements<Row>().SelectMany(row => row.Elements<Cell>()))
        {
            if (cell.CellReference?.Value is not string reference)
            {
                continue;
            }

            var value = ResolveCellValue(cell, sharedStrings);
            if (!string.IsNullOrEmpty(value))
            {
                cells[reference] = value;
            }
        }

        return new ExcelData(definedNames, cells);
    }

    private static string ResolveCellValue(Cell cell, string[] sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString && cell.CellValue is not null)
        {
            return sharedStrings[int.Parse(cell.CellValue.InnerText)];
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.Text?.Text ?? string.Empty;
        }

        return cell.CellValue?.InnerText ?? string.Empty;
    }

    private static string ResolveWorkbookPath() => FindUp(Path.Combine("docs", WorkbookFileName), WorkbookFileName);

    private static string ResolveSeedPath() => FindUp(Path.Combine("database", "seed", "001_master_catalogs.sql"), null);

    private static string FindUp(string relativePath, string? localFileName)
    {
        if (localFileName is not null)
        {
            var local = Path.Combine(AppContext.BaseDirectory, localFileName);
            if (File.Exists(local))
            {
                return local;
            }
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"No se encontró '{relativePath}' subiendo desde {AppContext.BaseDirectory}.");
        return string.Empty;
    }

    private sealed record ExcelData(Dictionary<string, string> DefinedNames, Dictionary<string, string> VariablesCells);
}
