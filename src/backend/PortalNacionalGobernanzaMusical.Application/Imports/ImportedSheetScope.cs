using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Qué hojas del archivo oficial entran en una importación.
/// <para>Se importan las de la ficha departamental —de <c>Identificación</c> a <c>Actores</c>—, que
/// son las que el Gestor diligencia. Las hojas de <c>Indicadores</c> y <c>Detalle Indicadores</c>
/// quedan fuera: sus filas provienen del catálogo maestro de indicadores y se gestionan desde el
/// módulo de Indicadores, no cargando el archivo.</para>
/// <para>La distinción no está escrita a mano sino tomada del Blueprint: esas dos hojas son las de
/// tipo <see cref="BlueprintSheetKinds.FixedCatalog"/>. Si mañana cambia el archivo oficial, el
/// alcance de la importación sigue siendo coherente con él.</para>
/// </summary>
public static class ImportedSheetScope
{
    /// <summary>¿Esta hoja se lee al importar?</summary>
    public static bool IsImported(BlueprintSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return sheet.Kind != BlueprintSheetKinds.FixedCatalog;
    }

    /// <summary>Hojas que se leen, validan y guardan al importar, en el orden del archivo.</summary>
    public static IReadOnlyList<BlueprintSheet> ImportedSheets(FichaBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        return blueprint.Sheets.Where(IsImported).ToArray();
    }

    /// <summary>Hojas que el archivo puede traer pero la importación ignora.</summary>
    public static IReadOnlyList<string> SkippedSheetNames(FichaBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        return blueprint.Sheets.Where(sheet => !IsImported(sheet)).Select(sheet => sheet.Name).ToArray();
    }
}
