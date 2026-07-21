using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Exports;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ExportsController(IExcelExportService exportService) : ControllerBase
{
    [HttpGet("fichas/{fichaId:guid}/excel")]
    public async Task<IActionResult> ExportFichaAsync(Guid fichaId, CancellationToken ct)
    {
        var bytes = await exportService.ExportFichaToExcelAsync(fichaId, ct);
        // Formato oficial macro-habilitado (.xlsm): preserva VBA, validaciones, estilos y fórmulas.
        return File(bytes, "application/vnd.ms-excel.sheet.macroEnabled.12", "ficha_departamental_gobernanza.xlsm");
    }
}
