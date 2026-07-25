using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

/// <summary>
/// Redacta las incidencias de importación en lenguaje funcional. Toma el código técnico y la
/// ubicación (hoja + celda), resuelve el nombre del campo en el Blueprint —el mismo que se ve en
/// la Ficha Departamental del portal— y arma: qué pasó, qué se recibió, qué se esperaba y cómo
/// corregirlo. El detalle técnico se conserva aparte, solo para soporte.
/// </summary>
public sealed class ImportIssueNarrator(BlueprintFieldLocator fieldLocator) : IImportIssueNarrator
{
    private const string EmptyValueLabel = "(vacío)";

    private sealed record Narrative(string Message, string? Expected, string? HowToFix);

    public ImportValidationIssueDto Narrate(ImportIssueSource issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        var context = ImportIssueContext.FromJson(issue.ContextJson);
        var columnLetter = BlueprintFieldLocator.ExtractColumnLetter(issue.CellReference);
        var field = fieldLocator.FindField(issue.SheetName, columnLetter);
        var fieldLabel = field?.Label;
        var narrative = Compose(issue, field, fieldLabel, columnLetter);

        return new ImportValidationIssueDto(
            issue.Id,
            issue.Severity,
            SeverityLabel(issue.Severity),
            issue.SheetName,
            issue.RowNumber,
            issue.CellReference,
            columnLetter,
            fieldLabel,
            issue.ErrorCode,
            BuildTitle(issue, fieldLabel),
            narrative.Message,
            NormalizeValue(issue.RawValue),
            context.Expected ?? narrative.Expected,
            context.HowToFix ?? narrative.HowToFix,
            context.TechnicalDetail);
    }

    /// <summary>Encabezado corto: "Fila 18 · Campo "Correo electrónico"".</summary>
    private static string BuildTitle(ImportIssueSource issue, string? fieldLabel)
    {
        var location = issue.RowNumber.HasValue ? $"Fila {issue.RowNumber}" : $"Hoja \"{issue.SheetName}\"";
        return string.IsNullOrWhiteSpace(fieldLabel)
            ? location
            : $"{location} · Campo \"{fieldLabel}\"";
    }

    private static string SeverityLabel(string severity)
    {
        return severity switch
        {
            ImportIssueCodes.SeverityError => "Debe corregirse",
            ImportIssueCodes.SeverityWarning => "Por revisar",
            _ => "Informativo"
        };
    }

    private static string NormalizeValue(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue) ? EmptyValueLabel : rawValue.Trim();
    }

    private Narrative Compose(ImportIssueSource issue, BlueprintField? field, string? fieldLabel, string? columnLetter)
    {
        var value = NormalizeValue(issue.RawValue);
        var sheet = issue.SheetName;
        var cell = string.IsNullOrWhiteSpace(issue.CellReference)
            ? $"la hoja \"{sheet}\""
            : $"la celda {issue.CellReference} de la hoja \"{sheet}\"";
        var fieldName = string.IsNullOrWhiteSpace(fieldLabel)
            ? (string.IsNullOrWhiteSpace(columnLetter) ? "este campo" : $"la columna {columnLetter}")
            : $"\"{fieldLabel}\"";

        return issue.ErrorCode switch
        {
            // ── Archivo: nombre, extensión y estructura ──────────────────────────────
            ImportIssueCodes.FileEmpty or
            ImportIssueCodes.FileExtensionInvalid or
            ImportIssueCodes.FileNameInvalid or
            ImportIssueCodes.FileTooLarge or
            ImportIssueCodes.FileNotReadable or
            ImportIssueCodes.SheetMissing or
            ImportIssueCodes.HeaderMismatch or
            ImportIssueCodes.WorkbookWithoutData =>
                new Narrative(issue.Message, null, null),

            // ── Identificación ──────────────────────────────────────────────────────
            ImportIssueCodes.IdentDateRequired => new Narrative(
                $"La fecha de levantamiento está vacía o no tiene un formato de fecha válido (se recibió {Quote(value)}).",
                "Una fecha real en formato dd/mm/aaaa, entre el 01/01/2000 y el 31/12/2100.",
                $"Escriba la fecha en {cell} usando el formato dd/mm/aaaa (por ejemplo 15/03/2026)."),

            ImportIssueCodes.IdentCityInvalid => new Narrative(
                $"La ciudad {Quote(value)} no pertenece al departamento registrado en esta fila.",
                "Un municipio del departamento seleccionado en la columna \"Departamento\".",
                $"Seleccione primero el departamento y luego elija la ciudad en la lista de {cell}: la lista se filtra automáticamente."),

            ImportIssueCodes.AxisComponentInvalid => new Narrative(
                $"El componente PNMC {Quote(value)} no corresponde al Eje PNMC seleccionado en esta fila.",
                "Un componente del eje elegido en la columna \"Eje PNMC\".",
                $"Seleccione primero el Eje PNMC y luego el componente en la lista de {cell}."),

            ImportIssueCodes.ActorRoleMappingMissing => new Narrative(
                $"No es posible validar los roles {Quote(value)} porque el campo \"Tipo de agente (categoría)\" no tiene un valor válido.",
                "Un tipo de agente de la lista y roles del ecosistema correspondientes a ese tipo.",
                "Corrija primero el tipo de agente de la fila y vuelva a seleccionar los roles del ecosistema."),

            ImportIssueCodes.ActorRoleInvalid => new Narrative(
                $"El rol en el ecosistema {Quote(value)} no corresponde al tipo de agente seleccionado en esta fila.",
                "Uno o varios roles del listado que se despliega para el tipo de agente elegido.",
                $"Seleccione los roles en {cell} usando la selección múltiple del archivo oficial (cada clic agrega o quita una opción)."),

            // ── Contacto de actores ─────────────────────────────────────────────────
            ImportIssueCodes.ActorEmailInvalid => new Narrative(
                $"El valor {Quote(value)} no corresponde a un correo electrónico válido.",
                "Un correo con el formato usuario@dominio.com.",
                $"Corrija el correo en {cell} incluyendo el signo @ y el dominio (por ejemplo nombre@entidad.gov.co)."),

            ImportIssueCodes.ActorPhoneFormat => new Narrative(
                $"El número {Quote(value)} no corresponde a un número de celular de 10 dígitos.",
                "Diez dígitos, sin espacios, guiones ni indicativos (por ejemplo 3001234567).",
                $"Escriba el número de contacto en {cell} usando solo los 10 dígitos del celular."),

            ImportIssueCodes.ActorPhoneLength => new Narrative(
                $"El número de contacto {Quote(value)} no tiene una longitud válida.",
                "Entre 7 y 20 caracteres (10 dígitos si es un celular).",
                $"Revise el número de contacto en {cell} y déjelo solo con dígitos."),

            // ── Años ────────────────────────────────────────────────────────────────
            ImportIssueCodes.IndicatorYearFormat or ImportIssueCodes.DetailYearFormat => new Narrative(
                $"El año {Quote(value)} no es un número válido.",
                "Un año de cuatro dígitos de la lista de años habilitados (por ejemplo 2026).",
                $"Seleccione el año en la lista desplegable de {cell}."),

            ImportIssueCodes.IndicatorYearInvalid or ImportIssueCodes.DetailYearInvalid => new Narrative(
                $"El año {Quote(value)} no está habilitado en el portal.",
                "Un año de la lista de años habilitados para el reporte de indicadores.",
                $"Seleccione el año en la lista desplegable de {cell}."),

            // ── Indicadores ─────────────────────────────────────────────────────────
            ImportIssueCodes.IndicatorNameInvalid or ImportIssueCodes.IndicatorNotFound or ImportIssueCodes.DetailIndicatorNotFound => new Narrative(
                $"El indicador {Quote(value)} no existe en el listado oficial de indicadores del PNMC.",
                "Uno de los indicadores precargados en la hoja (columna \"Nombre Indicador\"), que no debe modificarse.",
                "No edite ni reescriba los nombres de los indicadores: diligencie únicamente las columnas de avance, fuente, año y observaciones."),

            ImportIssueCodes.DetailTemplateNotFound => new Narrative(
                "La fila de detalle no corresponde a ninguna fórmula de cálculo registrada para el indicador.",
                "Las filas de detalle precargadas en la hoja \"Detalle Indicadores\" (columnas \"Fórmula de cálculo\" y \"Descripción / detalle\").",
                "No modifique las columnas precargadas de la hoja \"Detalle Indicadores\"; diligencie solo Departamento, MESES, Fuente, Año y Observaciones."),

            // ── Materialización de la ficha ─────────────────────────────────────────
            ImportIssueCodes.PersistIdentificationRequired => new Narrative(
                "No fue posible crear la ficha departamental porque la hoja \"Identificación\" no tiene una fila válida.",
                "La hoja \"Identificación\" con la fecha de levantamiento y el departamento diligenciados.",
                "Diligencie la fila 2 de la hoja \"Identificación\" (fecha de levantamiento y departamento) y vuelva a cargar el archivo."),

            ImportIssueCodes.PersistIdentificationMultiple => new Narrative(
                "La hoja \"Identificación\" tiene más de una fila diligenciada; se tomó la primera.",
                "Una sola fila por ficha departamental.",
                "Deje una única fila diligenciada en la hoja \"Identificación\" y vuelva a cargar el archivo si los datos usados no son los correctos."),

            ImportIssueCodes.PersistDiagnosticMultiple => new Narrative(
                "La hoja \"Diagnóstico ecosistema\" tiene más de una fila diligenciada; se tomó la primera.",
                "Una sola fila por ficha departamental.",
                "Deje una única fila diligenciada en la hoja \"Diagnóstico ecosistema\" y vuelva a cargar el archivo si los datos usados no son los correctos."),

            ImportIssueCodes.PersistSectionError => new Narrative(
                $"No fue posible guardar la información de la sección \"{sheet}\". Las demás secciones que sí se pudieron guardar quedaron registradas.",
                "Valores seleccionados de las listas del archivo oficial en todas las columnas de la sección.",
                $"Revise que las listas desplegables de la hoja \"{sheet}\" tengan valores del archivo oficial y vuelva a cargarlo. Si el problema persiste, comuníquese con el administrador del portal."),

            ImportIssueCodes.ImportException => new Narrative(
                "No fue posible procesar el archivo. La información no se importó.",
                $"El archivo oficial {ImportFileRules.OfficialFileName} sin hojas, filas ni columnas modificadas.",
                "Descargue de nuevo la plantilla oficial, copie allí la información diligenciada y vuelva a cargarla. Si el problema persiste, comuníquese con el administrador del portal."),

            // ── Listas y catálogos (mensaje genérico según el tipo de campo) ─────────
            _ => ComposeFallback(issue, field, fieldName, value, cell)
        };
    }

    /// <summary>
    /// Mensaje por defecto: si el campo es una lista del archivo oficial, se explica que el valor
    /// no está entre las opciones y se indica cómo seleccionarlo; en cualquier otro caso se emite
    /// un mensaje funcional genérico y el texto técnico queda como detalle de soporte.
    /// </summary>
    private static Narrative ComposeFallback(
        ImportIssueSource issue,
        BlueprintField? field,
        string fieldName,
        string value,
        string cell)
    {
        var isMultiSelect = field?.MultiSelect == true;
        var isList = field?.Type is BlueprintFieldTypes.List or BlueprintFieldTypes.DependentList;

        if (isList)
        {
            var expected = field?.InlineOptions is { Count: > 0 } options
                ? $"Una de estas opciones: {string.Join(", ", options)}."
                : $"Una de las opciones de la lista desplegable del campo {fieldName}.";

            var howToFix = isMultiSelect
                ? $"Seleccione las opciones en {cell} usando la selección múltiple del archivo oficial (cada clic agrega o quita una opción); no escriba los valores a mano."
                : $"Abra {cell} y seleccione un valor de la lista desplegable; no escriba el valor a mano.";

            var message = isMultiSelect
                ? $"La opción {Quote(value)} del campo {fieldName} no existe en el listado oficial del portal."
                : $"El valor {Quote(value)} del campo {fieldName} no existe en el listado oficial del portal.";

            return new Narrative(message, expected, howToFix);
        }

        return new Narrative(
            $"El valor {Quote(value)} del campo {fieldName} no pudo validarse.",
            $"Un valor diligenciado según las indicaciones del archivo oficial {ImportFileRules.OfficialFileName}.",
            $"Revise el dato en {cell} y vuelva a cargar el archivo.");
    }

    private static string Quote(string value) => $"\"{value}\"";
}
