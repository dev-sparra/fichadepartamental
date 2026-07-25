using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

/// <summary>
/// Aviso dirigido a una persona del portal. Se usa para comunicar los cambios de estado de una
/// ficha departamental (por ejemplo, cuando el Líder de Gobernanza aprueba o devuelve la ficha del
/// Gestor Departamental) sin depender de correo electrónico.
/// </summary>
public sealed class UserNotification : BaseEntity
{
    /// <summary>Correo del destinatario tal como se registró.</summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>Correo normalizado (mayúsculas) para consultar sin importar el formato.</summary>
    public string RecipientNormalizedEmail { get; set; } = string.Empty;

    /// <summary>Módulo al que pertenece el aviso (p. ej. "Gobernanza").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Evento que originó el aviso (p. ej. "FichaAprobada", "FichaDevuelta").</summary>
    public string EventCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Tono de presentación: success, warning, error o info.</summary>
    public string Tone { get; set; } = "info";

    /// <summary>Ruta del portal a la que debe llevar el aviso al hacer clic.</summary>
    public string? ActionRoute { get; set; }

    public string? RelatedEntityName { get; set; }
    public Guid? RelatedEntityId { get; set; }

    /// <summary>Persona que generó el cambio de estado (quien aprueba o devuelve).</summary>
    public string? TriggeredByEmail { get; set; }
    public string? TriggeredByName { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
