namespace PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

/// <summary>
/// Provee el <see cref="FichaBlueprint"/> canónico derivado del archivo oficial
/// <c>ficha_departamental_gobernanza.xlsm</c>.
/// </summary>
public interface IFichaBlueprintProvider
{
    FichaBlueprint GetBlueprint();
}
