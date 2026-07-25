using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Notifications;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Notifications;

/// <summary>
/// Bandeja de avisos del portal. Cada aviso pertenece a un destinatario (correo) y solo esa
/// persona lo consulta o lo marca como leído, sin importar su rol.
/// </summary>
public sealed class NotificationService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : INotificationService
{
    private const int MaxFeedSize = 50;

    public async Task NotifyAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Sin destinatario no hay aviso: la operación de negocio (aprobar/devolver) no debe fallar.
        if (string.IsNullOrWhiteSpace(command.RecipientEmail))
        {
            return;
        }

        var recipient = command.RecipientEmail.Trim();

        dbContext.Set<UserNotification>().Add(new UserNotification
        {
            RecipientEmail = recipient,
            RecipientNormalizedEmail = recipient.ToUpperInvariant(),
            Category = command.Category,
            EventCode = command.EventCode,
            Title = command.Title,
            Message = command.Message,
            Tone = command.Tone,
            ActionRoute = command.ActionRoute,
            RelatedEntityName = command.RelatedEntityName,
            RelatedEntityId = command.RelatedEntityId,
            TriggeredByEmail = currentUserService.Email,
            TriggeredByName = await ResolveDisplayNameAsync(currentUserService.Email, cancellationToken)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationFeedDto> GetFeedAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizedCurrentEmail();
        if (normalizedEmail is null)
        {
            return new NotificationFeedDto(0, []);
        }

        var pageSize = Math.Clamp(take, 1, MaxFeedSize);

        var query = dbContext.Set<UserNotification>().AsNoTracking()
            .Where(x => x.RecipientNormalizedEmail == normalizedEmail);

        var unreadCount = await query.CountAsync(x => !x.IsRead, cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(pageSize)
            .Select(x => new NotificationDto(
                x.Id,
                x.Category,
                x.EventCode,
                x.Title,
                x.Message,
                x.Tone,
                x.ActionRoute,
                x.RelatedEntityId,
                x.TriggeredByName,
                x.IsRead,
                x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new NotificationFeedDto(unreadCount, items);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizedCurrentEmail();

        var notification = await dbContext.Set<UserNotification>()
            .SingleOrDefaultAsync(x => x.Id == notificationId && x.RecipientNormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la notificación {notificationId} para el usuario actual.");

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        notification.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizedCurrentEmail();
        if (normalizedEmail is null)
        {
            return;
        }

        var pending = await dbContext.Set<UserNotification>()
            .Where(x => x.RecipientNormalizedEmail == normalizedEmail && !x.IsRead)
            .ToArrayAsync(cancellationToken);

        if (pending.Length == 0)
        {
            return;
        }

        foreach (var notification in pending)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            notification.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string? NormalizedCurrentEmail()
    {
        var email = currentUserService.Email;
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToUpperInvariant();
    }

    private async Task<string?> ResolveDisplayNameAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToUpperInvariant();
        var displayName = await dbContext.UserAccounts.AsNoTracking()
            .Where(x => x.NormalizedEmail == normalized)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(displayName) ? email : displayName;
    }
}
