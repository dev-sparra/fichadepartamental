using Microsoft.AspNetCore.Mvc;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "PortalNacionalGobernanzaMusical.API",
            utcNow = DateTime.UtcNow
        });
    }
}
