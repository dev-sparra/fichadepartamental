using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ImportsController(IWorkbookImportService workbookImportService, IImportTemplateService importTemplateService) : ControllerBase
{
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplateAsync(CancellationToken cancellationToken)
    {
        var bytes = await importTemplateService.GenerateTemplateAsync(cancellationToken);
        // Plantilla oficial en blanco (.xlsm): misma que se diligencia en la web, con macros y validaciones.
        return File(bytes, "application/vnd.ms-excel.sheet.macroEnabled.12", "ficha_departamental_gobernanza.xlsm");
    }

    [HttpPost("excel")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImportWorkbookResult>> ImportExcelAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntarse un archivo Excel válido.");
        }

        await using var stream = file.OpenReadStream();
        var result = await workbookImportService.ImportAsync(
            new ImportWorkbookCommand(file.FileName, file.ContentType, file.Length, stream),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyCollection<ImportBatchSummaryDto>>> GetBatchesAsync(CancellationToken cancellationToken)
    {
        return Ok(await workbookImportService.GetBatchesAsync(cancellationToken));
    }

    [HttpGet("batches/{batchId:guid}/issues")]
    public async Task<ActionResult<IReadOnlyCollection<ImportValidationIssueDto>>> GetIssuesAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return Ok(await workbookImportService.GetIssuesAsync(batchId, cancellationToken));
    }

    /// <summary>
    /// Elimina un lote de importación (y todos sus registros asociados).
    /// No revierte los datos que ya se persistieron en fichas/indicadores.
    /// </summary>
    [HttpDelete("batches/{batchId:guid}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> DeleteBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        await workbookImportService.DeleteBatchAsync(batchId, cancellationToken);
        return NoContent();
    }
}
