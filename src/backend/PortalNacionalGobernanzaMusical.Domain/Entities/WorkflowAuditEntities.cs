using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string UserEmail { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
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
