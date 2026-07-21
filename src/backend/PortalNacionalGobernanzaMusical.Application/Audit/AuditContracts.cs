namespace PortalNacionalGobernanzaMusical.Application.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string UserEmail,
    string UserDisplayName,
    string? IpAddress,
    string EntityName,
    Guid EntityId,
    string Operation,
    string? OldValuesJson,
    string? NewValuesJson,
    DateTime TimestampUtc);

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditLogDto>> GetLogsAsync(string? entityName = null, Guid? entityId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un cambio de auditoría para el usuario autenticado actual.
    /// El nombre para mostrar se resuelve desde el catálogo de usuarios en el momento del registro.
    /// </summary>
    Task LogAsync(string entityName, Guid entityId, string operation, string? oldValuesJson, string? newValuesJson, CancellationToken cancellationToken = default);
}
