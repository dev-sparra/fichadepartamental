namespace PortalNacionalGobernanzaMusical.Infrastructure.Exports;

/// <summary>
/// Datos a volcar en una hoja del libro, expresados por clave de campo del Blueprint.
/// El escritor resuelve la columna/tipo desde el Blueprint; aquí solo van los valores.
/// </summary>
public sealed record FichaExportSheet(string SheetKey, IReadOnlyList<FichaExportRow> Rows);

/// <summary>
/// Una fila a escribir. <see cref="Values"/> mapea la clave del campo del Blueprint al valor
/// (string para texto/lista/selección múltiple ya unida con ", ", DateOnly para fecha,
/// decimal/int para números). Los campos no editables (calculados/fijos) se ignoran.
/// </summary>
public sealed record FichaExportRow(int RowNumber, IReadOnlyDictionary<string, object?> Values);
