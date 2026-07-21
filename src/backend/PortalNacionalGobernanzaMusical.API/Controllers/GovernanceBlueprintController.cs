using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

/// <summary>
/// Expone el Blueprint canónico de la Ficha Departamental de Gobernanza (metadatos derivados
/// de <c>ficha_departamental_gobernanza.xlsm</c>). El frontend construye el formulario dinámico
/// a partir de esta definición; import y export comparten el mismo mapeo.
/// </summary>
[ApiController]
[Authorize]
[Route("api/governance/blueprint")]
public sealed class GovernanceBlueprintController(IFichaBlueprintProvider blueprintProvider) : ControllerBase
{
    [HttpGet]
    public ActionResult<FichaBlueprint> GetBlueprint()
    {
        return Ok(blueprintProvider.GetBlueprint());
    }
}
