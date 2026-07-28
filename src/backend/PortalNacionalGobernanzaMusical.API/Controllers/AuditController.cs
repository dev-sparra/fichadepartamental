using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

/// <summary>
/// Historial de auditoría. Solo lo consultan el Administrador y el Líder de Gobernanza: contiene
/// correos, direcciones IP y los valores anteriores y nuevos de cada cambio.
/// </summary>
[ApiController]
[Authorize(Roles = $"{SecurityRoleNames.Administrador},{SecurityRoleNames.LiderGobernanza}")]
[Route("api/[controller]")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogPageDto>> GetLogsAsync(
        [FromQuery] string? module,
        [FromQuery] string? userEmail,
        [FromQuery] string? operation,
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] string? result,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQuery
        {
            Module = module,
            UserEmail = userEmail,
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Result = result,
            Search = search,
            FromUtc = from,
            ToUtc = to,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await auditService.GetLogsAsync(query, cancellationToken));
    }

    /// <summary>Valores presentes en el historial para armar los filtros de la pantalla.</summary>
    [HttpGet("filters")]
    public async Task<ActionResult<AuditFilterOptionsDto>> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await auditService.GetFilterOptionsAsync(cancellationToken));
    }
}
