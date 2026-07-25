using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Resuelve hojas y campos del Blueprint a partir del nombre de hoja y la letra de columna del
/// Excel. Es el puente entre las incidencias técnicas (que guardan celda, p. ej. "G18") y el
/// nombre funcional del campo tal como se ve en la Ficha Departamental del portal.
/// </summary>
public sealed class BlueprintFieldLocator(IFichaBlueprintProvider blueprintProvider)
{
    private const string MonthLabelSeparator = " · ";

    private readonly FichaBlueprint _blueprint = blueprintProvider.GetBlueprint();

    public FichaBlueprint Blueprint => _blueprint;

    public BlueprintSheet? FindSheet(string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return null;
        }

        return _blueprint.Sheets.FirstOrDefault(sheet =>
            string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sheet.Key, sheetName, StringComparison.OrdinalIgnoreCase));
    }

    public BlueprintField? FindField(string? sheetName, string? columnLetter)
    {
        if (string.IsNullOrWhiteSpace(columnLetter))
        {
            return null;
        }

        return FindSheet(sheetName)?.Fields
            .FirstOrDefault(field => string.Equals(field.Column, columnLetter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Etiqueta del campo o <c>null</c> si la columna no está en el Blueprint.</summary>
    public string? FindFieldLabel(string? sheetName, string? columnLetter)
    {
        return FindField(sheetName, columnLetter)?.Label;
    }

    /// <summary>
    /// Extrae la letra de columna de una referencia de celda ("AF18" → "AF"). Devuelve
    /// <c>null</c> cuando la incidencia no apunta a una celda concreta.
    /// </summary>
    public static string? ExtractColumnLetter(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return null;
        }

        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        return letters.Length == 0 ? null : letters.ToUpperInvariant();
    }

    /// <summary>
    /// Encabezado que debe tener la columna en el Excel oficial. Para las columnas mensuales de
    /// la hoja Indicadores el Blueprint usa "Enero · Avance cuantitativo" (el mes viene del
    /// agrupador de la fila 1), mientras el encabezado real es solo "Avance cuantitativo".
    /// </summary>
    public static string ExpectedHeader(BlueprintField field)
    {
        var separator = field.Label.IndexOf(MonthLabelSeparator, StringComparison.Ordinal);
        return separator >= 0
            ? field.Label[(separator + MonthLabelSeparator.Length)..]
            : field.Label;
    }

    /// <summary>
    /// Reduce un encabezado leído del Excel a su etiqueta base: corta el sufijo
    /// "◉ Selección múltiple" (va después de un salto de línea) y elimina espacios de ancho cero
    /// que Excel arrastra al duplicar encabezados.
    /// </summary>
    public static string NormalizeHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }

        var text = header;
        var escapedBreak = text.IndexOf("_x000a_", StringComparison.OrdinalIgnoreCase);
        if (escapedBreak >= 0)
        {
            text = text[..escapedBreak];
        }

        var lineBreak = text.IndexOfAny(['\n', '\r']);
        if (lineBreak >= 0)
        {
            text = text[..lineBreak];
        }

        return text
            .Replace("​", string.Empty)
            .Replace("﻿", string.Empty)
            .Trim();
    }
}
