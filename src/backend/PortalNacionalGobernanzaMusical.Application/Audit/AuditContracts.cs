namespace PortalNacionalGobernanzaMusical.Application.Audit;

/// <summary>
/// Módulos del portal tal como se muestran en el historial de auditoría. Tenerlos centralizados
/// evita que la misma acción se registre con nombres distintos según el servicio que la escriba.
/// </summary>
public static class AuditModules
{
    public const string Autenticacion = "Autenticación";
    public const string Seguridad = "Seguridad";
    public const string Catalogos = "Catálogos";
    public const string Gobernanza = "Gobernanza";
    public const string Importaciones = "Importaciones";
    public const string Aprobaciones = "Aprobaciones";
    public const string Reportes = "Reportes";
}

/// <summary>Un campo que cambió, con su valor anterior y el nuevo, listos para mostrar.</summary>
public sealed record AuditChangeDto(string Field, string Label, string? Before, string? After);

/// <summary>Acción que se va a registrar en el historial.</summary>
public sealed record AuditEntry
{
    public required string Module { get; init; }
    public required string EntityName { get; init; }
    public required string Operation { get; init; }

    public Guid? EntityId { get; init; }
    public string? EntityKey { get; init; }

    /// <summary>Nombre del objeto afectado en palabras: "Ficha de Antioquia · 15/03/2026".</summary>
    public string? EntityLabel { get; init; }

    /// <summary>Qué ocurrió, redactado para una persona.</summary>
    public string? Description { get; init; }

    public string Result { get; init; } = "Exitoso";

    public IReadOnlyCollection<AuditChangeDto> Changes { get; init; } = [];

    public string? OldValuesJson { get; init; }
    public string? NewValuesJson { get; init; }

    /// <summary>
    /// Correo de quien ejecuta la acción. Solo hace falta cuando aún no hay sesión, como en el
    /// ingreso al portal; en el resto se toma del token.
    /// </summary>
    public string? UserEmailOverride { get; init; }
}

public sealed record AuditLogDto(
    Guid Id,
    string UserEmail,
    string UserDisplayName,
    string? UserRoles,
    string? IpAddress,
    string Module,
    string EntityName,
    Guid? EntityId,
    string? EntityKey,
    string? EntityLabel,
    string Operation,
    string? Description,
    string Result,
    IReadOnlyCollection<AuditChangeDto> Changes,
    string? RequestMethod,
    string? RequestPath,
    string? OldValuesJson,
    string? NewValuesJson,
    DateTime TimestampUtc);

/// <summary>Filtros del historial. Todos son opcionales y se combinan entre sí.</summary>
public sealed record AuditLogQuery
{
    public string? Module { get; init; }
    public string? UserEmail { get; init; }
    public string? Operation { get; init; }
    public string? EntityName { get; init; }
    public Guid? EntityId { get; init; }
    public string? Result { get; init; }

    /// <summary>Texto libre: busca en usuario, objeto afectado, acción y descripción.</summary>
    public string? Search { get; init; }

    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Página del historial con el total, para poder paginar en pantalla.</summary>
public sealed record AuditLogPageDto(
    IReadOnlyCollection<AuditLogDto> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>Valores disponibles para armar los filtros del historial.</summary>
public sealed record AuditFilterOptionsDto(
    IReadOnlyCollection<string> Modules,
    IReadOnlyCollection<string> Operations,
    IReadOnlyCollection<AuditUserOptionDto> Users);

public sealed record AuditUserOptionDto(string Email, string DisplayName);

public interface IAuditService
{
    Task<AuditLogPageDto> GetLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);

    /// <summary>Valores presentes en el historial para poblar los filtros de la pantalla.</summary>
    Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra una acción en el historial. El usuario, sus roles, la IP y la ruta de la petición
    /// se toman de la sesión actual, salvo que la acción indique otro correo.
    /// </summary>
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
