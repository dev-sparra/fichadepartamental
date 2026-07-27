using PortalNacionalGobernanzaMusical.Domain.Entities;

namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Decide qué filas de cada hoja pueden guardarse. La importación es parcial: lo que está bien se
/// guarda y solo se dejan fuera las filas con errores, de modo que un dato equivocado en una fila
/// no impida que la ficha quede disponible en Gobernanza.
/// </summary>
public static class ImportRowSelection
{
    /// <summary>
    /// Filas que sí se guardan de una sección. <see cref="SectionRows{T}.ShouldReplace"/> es
    /// <c>false</c> cuando la hoja traía filas pero todas tienen errores: en ese caso la sección no
    /// se reemplaza, para no borrar lo que ya estaba guardado de una carga anterior.
    /// </summary>
    public sealed record SectionRows<T>(IReadOnlyCollection<T> Rows, bool ShouldReplace);

    /// <summary>Hoja + número de fila de cada incidencia que impide guardar esa fila.</summary>
    public static IReadOnlySet<(string Sheet, int Row)> BuildRowsInErrorIndex(IEnumerable<ImportValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            .Where(issue => issue.Severity == ImportIssueCodes.SeverityError && issue.RowNumber.HasValue)
            .Select(issue => (issue.SheetName, issue.RowNumber!.Value))
            .ToHashSet();
    }

    public static SectionRows<T> SelectValidRows<T>(
        IReadOnlyCollection<T> rows,
        string sheetName,
        IReadOnlySet<(string Sheet, int Row)> rowsInError,
        Func<T, int> rowNumberSelector)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(rowsInError);
        ArgumentNullException.ThrowIfNull(rowNumberSelector);

        if (rows.Count == 0 || rowsInError.Count == 0)
        {
            return new SectionRows<T>(rows, true);
        }

        var valid = rows.Where(row => !rowsInError.Contains((sheetName, rowNumberSelector(row)))).ToArray();
        return new SectionRows<T>(valid, valid.Length > 0);
    }

    /// <summary>Indica si una fila concreta quedó descartada por errores.</summary>
    public static bool IsRowInError(IReadOnlySet<(string Sheet, int Row)> rowsInError, string sheetName, int rowNumber)
    {
        ArgumentNullException.ThrowIfNull(rowsInError);
        return rowsInError.Contains((sheetName, rowNumber));
    }
}
