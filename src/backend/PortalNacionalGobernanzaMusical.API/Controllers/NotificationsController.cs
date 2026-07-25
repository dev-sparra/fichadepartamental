using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalNacionalGobernanzaMusical.Application.Notifications;

namespace PortalNacionalGobernanzaMusical.API.Controllers;

/// <summary>
/// Bandeja de avisos del usuario autenticado. Cada persona ve únicamente sus propias
/// notificaciones (el filtro es el correo de la sesión, no el rol).
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationFeedDto>> GetFeedAsync([FromQuery] int take, CancellationToken cancellationToken)
    {
        return Ok(await notificationService.GetFeedAsync(take <= 0 ? 20 : take, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(notificationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }
}
