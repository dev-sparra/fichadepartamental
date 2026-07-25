using System.Text.RegularExpressions;

namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Reglas de aceptación del archivo de importación. Solo se admite el archivo oficial
/// <c>ficha_departamental_gobernanza.xlsm</c>: misma extensión y mismo nombre base (se tolera el
/// sufijo que agregan los navegadores al descargar dos veces, p. ej. "(1)", y un sufijo
/// descriptivo del territorio, p. ej. "ficha_departamental_gobernanza_antioquia.xlsm").
/// <para>Estas mismas reglas se replican en el frontend para avisar antes de subir el archivo.</para>
/// </summary>
public static partial class ImportFileRules
{
    /// <summary>Nombre del archivo oficial que se entrega como plantilla.</summary>
    public const string OfficialFileName = "ficha_departamental_gobernanza.xlsm";

    /// <summary>Nombre base exigido (sin extensión).</summary>
    public const string OfficialBaseName = "ficha_departamental_gobernanza";

    /// <summary>Única extensión admitida: el archivo oficial tiene macros.</summary>
    public const string OfficialExtension = ".xlsm";

    /// <summary>Tamaño máximo admitido (10 MB), alineado con el mensaje de la interfaz.</summary>
    public const long MaxFileSizeBytes = 10L * 1024 * 1024;

    [GeneratedRegex(@"^ficha[\s_-]*departamental[\s_-]*gobernanza([\s_\-(].*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OfficialNamePattern();

    public static bool HasOfficialExtension(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && Path.GetExtension(fileName).Equals(OfficialExtension, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasOfficialName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName).Trim();
        return OfficialNamePattern().IsMatch(baseName);
    }

    /// <summary>
    /// Valida nombre, extensión y tamaño. Devuelve las incidencias funcionales encontradas
    /// (lista vacía cuando el archivo es aceptable).
    /// </summary>
    public static IReadOnlyList<ImportFileRejection> Validate(string? fileName, long fileSizeBytes)
    {
        var rejections = new List<ImportFileRejection>();
        var displayName = string.IsNullOrWhiteSpace(fileName) ? "(sin nombre)" : fileName!.Trim();

        if (fileSizeBytes <= 0)
        {
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.FileEmpty,
                "El archivo seleccionado está vacío.",
                $"El archivo oficial {OfficialFileName} diligenciado.",
                "Verifica que el archivo se haya guardado con la información diligenciada y vuelve a cargarlo.",
                displayName));

            return rejections;
        }

        if (!HasOfficialExtension(fileName))
        {
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.FileExtensionInvalid,
                "El archivo seleccionado no corresponde al formato oficial de la Ficha Departamental de Gobernanza.",
                $"Un archivo con extensión {OfficialExtension} (libro de Excel con macros).",
                $"Por favor utilice el archivo oficial {OfficialFileName}. Puede descargarlo con el botón \"Descargar plantilla\".",
                displayName));
        }
        else if (!HasOfficialName(fileName))
        {
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.FileNameInvalid,
                $"El nombre del archivo no corresponde al de la Ficha Departamental de Gobernanza oficial.",
                $"Un archivo llamado {OfficialFileName}.",
                $"Renombre el archivo como {OfficialFileName} o descargue de nuevo la plantilla oficial y diligéncielas sobre ella.",
                displayName));
        }

        if (fileSizeBytes > MaxFileSizeBytes)
        {
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.FileTooLarge,
                "El archivo supera el tamaño máximo permitido.",
                $"Un archivo de máximo {MaxFileSizeBytes / (1024 * 1024)} MB.",
                "Elimine imágenes u hojas adicionales agregadas al archivo oficial y vuelva a cargarlo.",
                $"{Math.Round(fileSizeBytes / 1024d / 1024d, 1)} MB"));
        }

        return rejections;
    }
}

/// <summary>
/// Motivo funcional por el que un archivo no puede importarse. <paramref name="SheetName"/> se
/// completa cuando el problema es de una hoja concreta del libro y <paramref name="CellReference"/>
/// cuando se puede señalar la columna exacta.
/// </summary>
public sealed record ImportFileRejection(
    string Code,
    string Message,
    string? Expected,
    string? HowToFix,
    string? RawValue,
    string SheetName = "Archivo",
    int? RowNumber = null,
    string? CellReference = null,
    string? TechnicalDetail = null);
