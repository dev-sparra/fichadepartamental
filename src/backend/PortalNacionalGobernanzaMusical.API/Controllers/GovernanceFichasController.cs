using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Governance;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/governance/fichas")]
public sealed class GovernanceFichasController(IGovernanceFichaService governanceFichaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GovernanceFichaSummaryDto>>> GetFichasAsync(CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.GetFichasAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GovernanceFichaDetailDto>> GetFichaAsync(Guid id, CancellationToken cancellationToken)
    {
        var ficha = await governanceFichaService.GetFichaAsync(id, cancellationToken);
        return ficha is null ? NotFound() : Ok(ficha);
    }

    [HttpPost]
    public async Task<ActionResult<GovernanceFichaDetailDto>> CreateFichaAsync([FromBody] UpdateGovernanceFichaRequest request, CancellationToken cancellationToken)
    {
        var ficha = await governanceFichaService.CreateFichaAsync(request, cancellationToken);
        return Created($"/api/governance/fichas/{ficha.Id}", ficha);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GovernanceFichaDetailDto>> UpdateFichaAsync(Guid id, [FromBody] UpdateGovernanceFichaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.UpdateFichaAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFichaAsync(Guid id, CancellationToken cancellationToken)
    {
        await governanceFichaService.DeleteFichaAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/diagnostic")]
    public async Task<ActionResult<GovernanceDiagnosticDto>> GetDiagnosticAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await governanceFichaService.GetDiagnosticAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/diagnostic")]
    public async Task<ActionResult<GovernanceDiagnosticDto>> UpdateDiagnosticAsync(Guid id, [FromBody] GovernanceDiagnosticDto request, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.UpdateDiagnosticAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/opportunities")]
    public async Task<ActionResult<IReadOnlyCollection<GovernanceOpportunityDto>>> GetOpportunitiesAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.GetOpportunitiesAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/opportunities")]
    public async Task<ActionResult<IReadOnlyCollection<GovernanceOpportunityDto>>> ReplaceOpportunitiesAsync(Guid id, [FromBody] IReadOnlyCollection<GovernanceOpportunityDto> request, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.ReplaceOpportunitiesAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/pnmc-axes")]
    public async Task<ActionResult<IReadOnlyCollection<GovernancePnmcAxisDto>>> GetPnmcAxesAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.GetPnmcAxesAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/pnmc-axes")]
    public async Task<ActionResult<IReadOnlyCollection<GovernancePnmcAxisDto>>> ReplacePnmcAxesAsync(Guid id, [FromBody] IReadOnlyCollection<GovernancePnmcAxisDto> request, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.ReplacePnmcAxesAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/actors")]
    public async Task<ActionResult<IReadOnlyCollection<GovernanceActorDto>>> GetActorsAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.GetActorsAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/actors")]
    public async Task<ActionResult<IReadOnlyCollection<GovernanceActorDto>>> ReplaceActorsAsync(Guid id, [FromBody] IReadOnlyCollection<GovernanceActorDto> request, CancellationToken cancellationToken)
    {
        return Ok(await governanceFichaService.ReplaceActorsAsync(id, request, cancellationToken));
    }
}
