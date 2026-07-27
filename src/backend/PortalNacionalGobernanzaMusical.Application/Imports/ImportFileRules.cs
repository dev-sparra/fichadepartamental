namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Reglas de aceptación del archivo de importación. Lo que se exige es el <b>formato</b>: un libro
/// <c>.xlsm</c> con la estructura de la Ficha Departamental de Gobernanza (hojas y columnas), que
/// verifica <c>WorkbookStructureValidator</c>.
/// <para>El <b>nombre del archivo es libre</b>: es habitual que en territorio se renombre el
/// archivo (por departamento, fecha o versión) sin alterar su contenido, y rechazarlo por el
/// nombre bloqueaba importaciones perfectamente válidas.</para>
/// <para>Estas mismas reglas se replican en el frontend para avisar antes de subir el archivo.</para>
/// </summary>
public static class ImportFileRules
{
    /// <summary>Nombre del archivo oficial que se entrega como plantilla (referencia para los mensajes).</summary>
    public const string OfficialFileName = "ficha_departamental_gobernanza.xlsm";

    /// <summary>Única extensión admitida: el archivo oficial tiene macros.</summary>
    public const string OfficialExtension = ".xlsm";

    /// <summary>Tamaño máximo admitido (10 MB), alineado con el mensaje de la interfaz.</summary>
    public const long MaxFileSizeBytes = 10L * 1024 * 1024;

    public static bool HasOfficialExtension(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && Path.GetExtension(fileName).Equals(OfficialExtension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Valida extensión y tamaño (el nombre es libre). Devuelve las incidencias funcionales
    /// encontradas; lista vacía cuando el archivo es aceptable.
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
                $"Un archivo con extensión {OfficialExtension} (libro de Excel con macros). El nombre del archivo puede ser cualquiera.",
                $"Diligencie la información sobre el archivo oficial {OfficialFileName} y cárguelo sin convertirlo a otro formato. Puede descargarlo con el botón \"Descargar plantilla\".",
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
