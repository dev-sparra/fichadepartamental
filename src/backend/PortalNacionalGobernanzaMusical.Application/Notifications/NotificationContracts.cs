namespace PortalNacionalGobernanzaMusical.Application.Notifications;

/// <summary>Categorías de aviso (módulo de origen).</summary>
public static class NotificationCategories
{
    public const string Gobernanza = "Gobernanza";
    public const string Importaciones = "Importaciones";
}

/// <summary>Eventos que generan aviso al Gestor Departamental.</summary>
public static class NotificationEvents
{
    public const string FichaAprobada = "FichaAprobada";
    public const string FichaDevuelta = "FichaDevuelta";
}

public static class NotificationTones
{
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Info = "info";
}

public sealed record NotificationDto(
    Guid Id,
    string Category,
    string EventCode,
    string Title,
    string Message,
    string Tone,
    string? ActionRoute,
    Guid? RelatedEntityId,
    string? TriggeredByName,
    bool IsRead,
    DateTime CreatedAtUtc);

/// <summary>Bandeja del usuario autenticado: avisos recientes y cuántos están sin leer.</summary>
public sealed record NotificationFeedDto(
    int UnreadCount,
    IReadOnlyCollection<NotificationDto> Items);

public sealed record CreateNotificationCommand(
    string RecipientEmail,
    string Category,
    string EventCode,
    string Title,
    string Message,
    string Tone,
    string? ActionRoute = null,
    string? RelatedEntityName = null,
    Guid? RelatedEntityId = null);

public interface INotificationService
{
    /// <summary>Registra un aviso para una persona. No falla la operación de negocio si no hay destinatario.</summary>
    Task NotifyAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default);

    Task<NotificationFeedDto> GetFeedAsync(int take = 20, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}
