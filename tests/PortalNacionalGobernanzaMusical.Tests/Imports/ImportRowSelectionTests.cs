using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Domain.Entities;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// La importación es parcial: las filas correctas se guardan y solo quedan fuera las que tienen
/// errores, para que un dato equivocado no impida ver la ficha en Gobernanza.
/// </summary>
public sealed class ImportRowSelectionTests
{
    private sealed record Row(int SourceRowNumber, string Nombre);

    private static ImportValidationIssue Issue(string sheet, int? row, string severity) => new()
    {
        SheetName = sheet,
        RowNumber = row,
        Severity = severity,
        ErrorCode = "TEST",
        Message = "test"
    };

    [Fact]
    public void BuildRowsInErrorIndex_ShouldOnlyTakeErrorsWithRow()
    {
        var issues = new[]
        {
            Issue("Actores", 4, ImportIssueCodes.SeverityError),
            Issue("Actores", 5, ImportIssueCodes.SeverityWarning),
            Issue("Actores", null, ImportIssueCodes.SeverityError),
            Issue("Ejes PNMC", 2, ImportIssueCodes.SeverityError)
        };

        var index = ImportRowSelection.BuildRowsInErrorIndex(issues);

        Assert.Equal(2, index.Count);
        Assert.Contains(("Actores", 4), index);
        Assert.Contains(("Ejes PNMC", 2), index);
    }

    [Fact]
    public void SelectValidRows_ShouldKeepTheRowsWithoutErrors()
    {
        Row[] rows = [new(2, "Casa de la cultura"), new(3, "Escuela"), new(4, "Fundación")];
        var rowsInError = ImportRowSelection.BuildRowsInErrorIndex([Issue("Actores", 3, ImportIssueCodes.SeverityError)]);

        var section = ImportRowSelection.SelectValidRows(rows, "Actores", rowsInError, row => row.SourceRowNumber);

        Assert.True(section.ShouldReplace);
        Assert.Equal([2, 4], section.Rows.Select(row => row.SourceRowNumber));
    }

    [Fact]
    public void SelectValidRows_WhenEveryRowHasErrors_ShouldNotReplaceTheSection()
    {
        Row[] rows = [new(2, "Casa de la cultura"), new(3, "Escuela")];
        var rowsInError = ImportRowSelection.BuildRowsInErrorIndex(
        [
            Issue("Actores", 2, ImportIssueCodes.SeverityError),
            Issue("Actores", 3, ImportIssueCodes.SeverityError)
        ]);

        var section = ImportRowSelection.SelectValidRows(rows, "Actores", rowsInError, row => row.SourceRowNumber);

        // No se reemplaza: se conserva lo que ya estaba guardado de una carga anterior.
        Assert.False(section.ShouldReplace);
        Assert.Empty(section.Rows);
    }

    [Fact]
    public void SelectValidRows_ErrorsInAnotherSheet_ShouldNotAffectThisOne()
    {
        Row[] rows = [new(2, "Eje 1")];
        var rowsInError = ImportRowSelection.BuildRowsInErrorIndex([Issue("Actores", 2, ImportIssueCodes.SeverityError)]);

        var section = ImportRowSelection.SelectValidRows(rows, "Ejes PNMC", rowsInError, row => row.SourceRowNumber);

        Assert.True(section.ShouldReplace);
        Assert.Single(section.Rows);
    }

    [Fact]
    public void SelectValidRows_EmptySheet_ShouldReplaceSoDeletionsAreApplied()
    {
        var section = ImportRowSelection.SelectValidRows<Row>([], "Actores", new HashSet<(string, int)>(), row => row.SourceRowNumber);

        Assert.True(section.ShouldReplace);
        Assert.Empty(section.Rows);
    }
}
