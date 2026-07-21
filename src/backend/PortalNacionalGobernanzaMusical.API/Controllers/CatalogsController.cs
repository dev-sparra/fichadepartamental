using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Catalogs;
using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CatalogsController(ICatalogQueryService catalogQueryService, ICatalogAdminService catalogAdminService) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentCatalogOptionDto>>> GetDepartmentsAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetDepartmentsAsync(cancellationToken));
    }

    [HttpGet("departments/{departmentId:int}/municipalities")]
    public async Task<ActionResult<IReadOnlyCollection<MunicipalityCatalogOptionDto>>> GetMunicipalitiesAsync(int departmentId, CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetMunicipalitiesAsync(departmentId, cancellationToken));
    }

    [HttpGet("lookups/{lookupName}")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogOptionDto>>> GetLookupAsync(string lookupName, CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetLookupAsync(lookupName, cancellationToken));
    }

    [HttpGet("pnmc-axes/{pnmcAxisId:int}/components")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogOptionDto>>> GetPnmcComponentsAsync(int pnmcAxisId, CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetPnmcComponentsAsync(pnmcAxisId, cancellationToken));
    }

    [HttpGet("agent-types/{agentTypeId:int}/ecosystem-roles")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogOptionDto>>> GetEcosystemRolesAsync(int agentTypeId, CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetEcosystemRolesAsync(agentTypeId, cancellationToken));
    }

    [HttpGet("indicator-definitions")]
    public async Task<ActionResult<IReadOnlyCollection<IndicatorDefinitionCatalogDto>>> GetIndicatorDefinitionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogQueryService.GetIndicatorDefinitionsAsync(cancellationToken));
    }

    [HttpGet("admin/definitions")]
    [Authorize(Roles = Roles.Administrador)]
    public ActionResult<IReadOnlyCollection<CatalogDefinitionDto>> GetCatalogDefinitions()
    {
        return Ok(catalogAdminService.GetCatalogDefinitions());
    }

    [HttpGet("admin/{catalogKey}/items")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogItemDto>>> GetCatalogItemsAsync(string catalogKey, [FromQuery] int? parentId, CancellationToken cancellationToken)
    {
        return Ok(await catalogAdminService.GetItemsAsync(catalogKey, parentId, cancellationToken));
    }

    [HttpPost("admin/{catalogKey}/items")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<CatalogItemDto>> CreateCatalogItemAsync(string catalogKey, [FromBody] UpsertCatalogItemRequest request, CancellationToken cancellationToken)
    {
        var item = await catalogAdminService.CreateAsync(catalogKey, request, cancellationToken);
        return CreatedAtAction(nameof(GetCatalogItemsAsync), new { catalogKey }, item);
    }

    [HttpPut("admin/{catalogKey}/items/{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<CatalogItemDto>> UpdateCatalogItemAsync(string catalogKey, int id, [FromBody] UpsertCatalogItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogAdminService.UpdateAsync(catalogKey, id, request, cancellationToken));
    }

    [HttpDelete("admin/{catalogKey}/items/{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> DeleteCatalogItemAsync(string catalogKey, int id, CancellationToken cancellationToken)
    {
        await catalogAdminService.DeleteAsync(catalogKey, id, cancellationToken);
        return NoContent();
    }
}
