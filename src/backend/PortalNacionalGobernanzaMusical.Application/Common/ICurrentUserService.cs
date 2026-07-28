namespace PortalNacionalGobernanzaMusical.Application.Common;

public interface ICurrentUserService
{
    string? Email { get; }

    /// <summary>Dirección IP del cliente de la petición actual (para auditoría).</summary>
    string? IpAddress { get; }

    /// <summary>Roles del usuario actual obtenidos del token JWT.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Verbo HTTP de la petición actual (para auditoría).</summary>
    string? RequestMethod { get; }

    /// <summary>Ruta de la petición actual, sin la cadena de consulta (para auditoría).</summary>
    string? RequestPath { get; }

    /// <summary>Verifica si el usuario actual tiene alguno de los roles especificados.</summary>
    bool HasAnyRole(params string[] roles);
}
