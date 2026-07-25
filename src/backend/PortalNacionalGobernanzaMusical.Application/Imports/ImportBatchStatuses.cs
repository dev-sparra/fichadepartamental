namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Códigos internos de estado de un lote de importación. Se guardan en
/// <c>import_batches.status</c> y nunca se muestran al usuario: la interfaz siempre presenta la
/// etiqueta funcional resuelta por <see cref="ImportStatusCatalog"/>.
/// </summary>
public static class ImportBatchStatuses
{
    /// <summary>El archivo se está revisando (nombre, extensión, hojas y encabezados).</summary>
    public const string Validating = "Validating";

    /// <summary>La estructura es correcta y se están leyendo y validando los datos.</summary>
    public const string Processing = "Processing";

    /// <summary>Todo se importó sin incidencias.</summary>
    public const string Completed = "Completed";

    /// <summary>Se importó todo, pero hay valores que conviene revisar.</summary>
    public const string CompletedWithWarnings = "CompletedWithWarnings";

    /// <summary>Se importó parcialmente: hay filas con errores que quedaron fuera.</summary>
    public const string CompletedWithErrors = "CompletedWithErrors";

    /// <summary>El archivo no corresponde al formato oficial: no se importó nada.</summary>
    public const string Rejected = "Rejected";

    /// <summary>Estado histórico anterior a los estados funcionales; se presenta como rechazado.</summary>
    public const string Failed = "Failed";
}

/// <summary>Tonos de presentación que el frontend traduce a color y iconografía.</summary>
public static class ImportStatusTones
{
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Progress = "progress";
    public const string Info = "info";
}

/// <summary>
/// Estado de un lote expresado para el usuario: qué ocurrió y cuál es el siguiente paso.
/// </summary>
public sealed record ImportStatusPresentation(
    string Code,
    string Label,
    string Description,
    string NextStep,
    string Tone);

/// <summary>
/// Traduce los códigos de estado a lenguaje funcional. Es la única fuente de verdad de las
/// etiquetas de estado: la usan el resultado de la carga, el historial de lotes y la interfaz.
/// </summary>
public static class ImportStatusCatalog
{
    private static readonly Dictionary<string, ImportStatusPresentation> Presentations = new(StringComparer.OrdinalIgnoreCase)
    {
        [ImportBatchStatuses.Validating] = new(
            ImportBatchStatuses.Validating,
            "Archivo en validación",
            "Estamos verificando que el archivo corresponda a la Ficha Departamental de Gobernanza oficial (hojas y columnas requeridas).",
            "Espera unos segundos; no cierres esta pantalla.",
            ImportStatusTones.Progress),

        [ImportBatchStatuses.Processing] = new(
            ImportBatchStatuses.Processing,
            "Procesando archivo",
            "El archivo es válido y se están leyendo y revisando los datos diligenciados.",
            "Espera a que termine el procesamiento para ver el resultado.",
            ImportStatusTones.Progress),

        [ImportBatchStatuses.Completed] = new(
            ImportBatchStatuses.Completed,
            "Importación exitosa",
            "Los datos se importaron correctamente y ya están disponibles en el módulo de Gobernanza.",
            "Abre el módulo de Gobernanza para consultar y completar la ficha del departamento.",
            ImportStatusTones.Success),

        [ImportBatchStatuses.CompletedWithWarnings] = new(
            ImportBatchStatuses.CompletedWithWarnings,
            "Importación completada con observaciones",
            "Los datos se importaron, pero hay valores que conviene revisar porque no coinciden exactamente con los listados oficiales.",
            "Revisa las observaciones de abajo y ajusta esos valores desde el módulo de Gobernanza o vuelve a cargar el archivo corregido.",
            ImportStatusTones.Warning),

        [ImportBatchStatuses.CompletedWithErrors] = new(
            ImportBatchStatuses.CompletedWithErrors,
            "Importación completada con observaciones",
            "El archivo se procesó, pero algunas filas no se importaron porque tienen datos que deben corregirse.",
            "Corrige en el archivo oficial las filas indicadas abajo y vuelve a cargarlo; se actualizará la información sin duplicarla.",
            ImportStatusTones.Warning),

        [ImportBatchStatuses.Rejected] = new(
            ImportBatchStatuses.Rejected,
            "Importación rechazada",
            "El archivo no corresponde al formato oficial de la Ficha Departamental de Gobernanza, por lo que no se importó ningún dato.",
            "Descarga la plantilla oficial ficha_departamental_gobernanza.xlsm, diligénciala y vuelve a cargarla.",
            ImportStatusTones.Error),

        [ImportBatchStatuses.Failed] = new(
            ImportBatchStatuses.Rejected,
            "Importación rechazada",
            "La carga no pudo completarse y no se importó ningún dato.",
            "Verifica que estés usando el archivo oficial ficha_departamental_gobernanza.xlsm y vuelve a intentarlo.",
            ImportStatusTones.Error)
    };

    public static ImportStatusPresentation Resolve(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) && Presentations.TryGetValue(status, out var presentation))
        {
            return presentation;
        }

        return new ImportStatusPresentation(
            status ?? string.Empty,
            "Estado en revisión",
            "La carga quedó registrada, pero su estado no pudo interpretarse.",
            "Vuelve a cargar el archivo oficial o comunícate con el administrador del portal.",
            ImportStatusTones.Info);
    }

    /// <summary>
    /// Determina el estado final del lote a partir del resultado de la validación de datos.
    /// </summary>
    public static string ResolveFinalStatus(bool hasErrors, bool hasWarnings)
    {
        if (hasErrors)
        {
            return ImportBatchStatuses.CompletedWithErrors;
        }

        return hasWarnings ? ImportBatchStatuses.CompletedWithWarnings : ImportBatchStatuses.Completed;
    }
}
