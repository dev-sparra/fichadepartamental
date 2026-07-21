namespace PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

/// <summary>
/// Tipos de campo del Blueprint. Se exponen como cadenas (unión de strings en TypeScript)
/// para mantener un contrato estable y legible en el frontend.
/// </summary>
public static class BlueprintFieldTypes
{
    public const string Text = "text";
    public const string Date = "date";
    public const string Integer = "integer";
    public const string Decimal = "decimal";
    public const string List = "list";
    public const string DependentList = "dependentList";
    public const string Calculated = "calculated";
    public const string Fixed = "fixed";
}

/// <summary>Naturaleza de la hoja respecto al diligenciamiento.</summary>
public static class BlueprintSheetKinds
{
    /// <summary>Una sola fila por ficha (Identificación, Diagnóstico).</summary>
    public const string SingleRow = "singleRow";

    /// <summary>N filas (Oportunidades, Ejes PNMC, Actores).</summary>
    public const string Collection = "collection";

    /// <summary>Filas fijas provenientes de un catálogo (Indicadores, Detalle).</summary>
    public const string FixedCatalog = "fixedCatalog";
}

/// <summary>
/// Claves de cascada que corresponden a endpoints existentes en <c>CatalogsController</c>
/// para resolver las listas dependientes (equivalen a las validaciones INDIRECT del Excel).
/// </summary>
public static class BlueprintCascades
{
    public const string MunicipalitiesByDepartment = "municipalitiesByDepartment"; // GET catalogs/departments/{id}/municipalities
    public const string ComponentsByAxis = "componentsByAxis";                     // GET catalogs/pnmc-axes/{id}/components
    public const string RolesByAgentType = "rolesByAgentType";                     // GET catalogs/agent-types/{id}/ecosystem-roles
}

/// <summary>Reglas de validación derivadas de las validaciones del Excel.</summary>
public sealed record BlueprintValidation
{
    public bool Required { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? DateMin { get; init; }
    public string? DateMax { get; init; }

    /// <summary>Fórmula de validación personalizada original del Excel (informativa; ej. correo electrónico).</summary>
    public string? ExcelFormula { get; init; }

    /// <summary>Mensaje de error equivalente al del Excel.</summary>
    public string? Message { get; init; }
}

/// <summary>Definición de un campo (columna) de una hoja.</summary>
public sealed record BlueprintField
{
    /// <summary>Letra de columna en Excel (ej. "A", "AB").</summary>
    public required string Column { get; init; }

    /// <summary>Índice de columna 1-based (A = 1).</summary>
    public required int ColumnIndex { get; init; }

    /// <summary>Clave camelCase alineada con la propiedad del DTO correspondiente.</summary>
    public required string Key { get; init; }

    /// <summary>Etiqueta base del campo (sin el sufijo "◉ Selección múltiple").</summary>
    public required string Label { get; init; }

    /// <summary>Uno de <see cref="BlueprintFieldTypes"/>.</summary>
    public required string Type { get; init; }

    /// <summary>Editable por el usuario (false para columnas calculadas o de catálogo fijo).</summary>
    public bool Editable { get; init; } = true;

    /// <summary>Selección múltiple gestionada por la macro <c>Workbook_SheetChange</c>.</summary>
    public bool MultiSelect { get; init; }

    /// <summary>Separador usado por la macro al serializar selección múltiple en la celda.</summary>
    public string? MultiSeparator { get; init; }

    /// <summary>Nombre del rango <c>Multi_*</c> asociado (para import/export y verificación).</summary>
    public string? MultiRange { get; init; }

    /// <summary>Clave de catálogo/lookup del API (ej. "departments", "region-ocad").</summary>
    public string? Catalog { get; init; }

    /// <summary>Nombre del rango con nombre del Excel que respalda la lista (ej. "Departamentos").</summary>
    public string? ExcelRange { get; init; }

    /// <summary>Opciones fijas para listas inline (ej. Existe / No existe / Por renovar).</summary>
    public IReadOnlyList<string>? InlineOptions { get; init; }

    /// <summary>Columna padre de la que depende (listas dependientes).</summary>
    public string? DependsOnColumn { get; init; }

    /// <summary>Estrategia de cascada (uno de <see cref="BlueprintCascades"/>).</summary>
    public string? Cascade { get; init; }

    /// <summary>Fórmula original del Excel (para columnas calculadas; informativa).</summary>
    public string? Formula { get; init; }

    /// <summary>Texto de ayuda (input prompt) original del Excel.</summary>
    public string? Prompt { get; init; }

    public BlueprintValidation? Validation { get; init; }
}

/// <summary>Definición de una hoja del libro.</summary>
public sealed record BlueprintSheet
{
    /// <summary>Nombre exacto de la hoja en el Excel (respetando acentos).</summary>
    public required string Name { get; init; }

    /// <summary>Slug estable para rutas/keys de UI.</summary>
    public required string Key { get; init; }

    /// <summary>Nombre del ListObject (tabla) o null si la hoja no tiene tabla.</summary>
    public string? Table { get; init; }

    /// <summary>Rango total de la hoja (ej. "A1:G51").</summary>
    public required string Range { get; init; }

    public required int HeaderRow { get; init; }
    public required int DataStartRow { get; init; }
    public required int DataEndRow { get; init; }

    /// <summary>Uno de <see cref="BlueprintSheetKinds"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>Rol que puede diligenciar la hoja (uno de <c>Roles</c>).</summary>
    public required string EditableByRole { get; init; }

    public required IReadOnlyList<BlueprintField> Fields { get; init; }
}

/// <summary>
/// Metadatos completos de la Ficha Departamental de Gobernanza derivados de
/// <c>ficha_departamental_gobernanza.xlsm</c>. Fuente única de verdad compartida por
/// el formulario dinámico, la validación, la importación y la exportación.
/// </summary>
public sealed record FichaBlueprint
{
    public required string Version { get; init; }
    public required string SourceWorkbook { get; init; }

    /// <summary>Separador de selección múltiple usado por la macro del Excel ("," + espacio).</summary>
    public required string MultiSelectSeparator { get; init; }

    public required IReadOnlyList<BlueprintSheet> Sheets { get; init; }
}
