using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Indicators;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class IndicatorsController(IIndicatorQueryService indicatorQueryService) : ControllerBase
{
    [HttpGet("records")]
    public async Task<ActionResult<IReadOnlyCollection<IndicatorRecordDto>>> GetRecordsAsync(CancellationToken cancellationToken)
    {
        return Ok(await indicatorQueryService.GetIndicatorRecordsAsync(cancellationToken));
    }

    [HttpGet("records/{id:guid}")]
    public async Task<ActionResult<IndicatorRecordDto>> GetRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await indicatorQueryService.GetIndicatorRecordAsync(id, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPut("records/{id:guid}")]
    public async Task<ActionResult<IndicatorRecordDto>> UpdateRecordAsync(Guid id, [FromBody] UpdateIndicatorRecordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await indicatorQueryService.UpdateIndicatorRecordAsync(id, request, cancellationToken));
    }

    [HttpPost("worksheet")]
    public async Task<ActionResult<IndicatorWorksheetDto>> ProvisionWorksheetAsync([FromBody] ProvisionWorksheetRequest request, CancellationToken cancellationToken)
    {
        return Ok(await indicatorQueryService.ProvisionWorksheetAsync(request.DepartmentId, request.Year, cancellationToken));
    }

    [HttpGet("details")]
    public async Task<ActionResult<IReadOnlyCollection<IndicatorDetailRecordDto>>> GetDetailsAsync(CancellationToken cancellationToken)
    {
        return Ok(await indicatorQueryService.GetDetailRecordsAsync(cancellationToken));
    }

    [HttpGet("details/{id:guid}")]
    public async Task<ActionResult<IndicatorDetailRecordDto>> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var detail = await indicatorQueryService.GetDetailRecordAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPut("details/{id:guid}")]
    public async Task<ActionResult<IndicatorDetailRecordDto>> UpdateDetailAsync(Guid id, [FromBody] UpdateIndicatorDetailRecordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await indicatorQueryService.UpdateDetailRecordAsync(id, request, cancellationToken));
    }
}
