using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Audit;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyCollection<AuditLogDto>>> GetLogsAsync([FromQuery] string? entityName, [FromQuery] Guid? entityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) => Ok(await auditService.GetLogsAsync(entityName, entityId, page, pageSize, ct));
}
