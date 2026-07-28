using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

/// <summary>
/// Una acción registrada en el historial de auditoría: quién la hizo, en qué módulo, sobre qué
/// objeto, qué cambió exactamente y desde dónde.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    public string UserEmail { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;

    /// <summary>Roles con los que actuaba el usuario en ese momento, separados por coma.</summary>
    public string? UserRoles { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Módulo del portal donde ocurrió la acción (Gobernanza, Seguridad, Catálogos…).</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Tipo de objeto afectado, como lo nombra el dominio (FichaDepartamental, UserAccount…).</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Identificador del objeto cuando es una entidad con Guid.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Identificador del objeto cuando no es un Guid (por ejemplo, el id de un catálogo).</summary>
    public string? EntityKey { get; set; }

    /// <summary>Nombre del objeto en palabras, para leer el historial sin abrir el detalle.</summary>
    public string? EntityLabel { get; set; }

    public string Operation { get; set; } = string.Empty;

    /// <summary>Qué ocurrió, redactado para una persona.</summary>
    public string? Description { get; set; }

    /// <summary>Si la acción se completó (<c>Exitoso</c>) o no (<c>Fallido</c>).</summary>
    public string Result { get; set; } = AuditResults.Exitoso;

    /// <summary>Cambios campo a campo en formato JSON: etiqueta, valor anterior y valor nuevo.</summary>
    public string? ChangesJson { get; set; }

    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
}

/// <summary>Resultado de una acción auditada.</summary>
public static class AuditResults
{
    public const string Exitoso = "Exitoso";
    public const string Fallido = "Fallido";
}

public sealed class ApprovalRecord : BaseEntity
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }
    public string Status { get; set; } = "Borrador";
    public string? ReviewedByEmail { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? Comment { get; set; }
}
