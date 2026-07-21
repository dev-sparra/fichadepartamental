using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Workflow;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/governance/fichas/{fichaId:guid}/workflow")]
public sealed class WorkflowController(IWorkflowService workflowService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<FichaApprovalStatusDto>> GetStatusAsync(Guid fichaId, CancellationToken ct) => Ok(await workflowService.GetStatusAsync(fichaId, ct));
    [HttpPost("approve")] public async Task<ActionResult<FichaApprovalStatusDto>> ApproveAsync(Guid fichaId, [FromBody] string? comment, CancellationToken ct) => Ok(await workflowService.ApproveAsync(new ApprovalActionDto(fichaId, "Aprobado", comment), ct));
    [HttpPost("reject")] public async Task<ActionResult<FichaApprovalStatusDto>> RejectAsync(Guid fichaId, [FromBody] string? comment, CancellationToken ct) => Ok(await workflowService.RejectAsync(new ApprovalActionDto(fichaId, "Devuelto", comment), ct));
    [HttpGet("history")] public async Task<ActionResult<IReadOnlyCollection<ApprovalRecordDto>>> GetHistoryAsync(Guid fichaId, CancellationToken ct) => Ok(await workflowService.GetHistoryAsync(fichaId, ct));
}
