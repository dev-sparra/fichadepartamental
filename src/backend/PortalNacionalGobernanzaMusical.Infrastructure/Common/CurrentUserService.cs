using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PortalNacionalGobernanzaMusical.Application.Common;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Common;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? RequestMethod => httpContextAccessor.HttpContext?.Request.Method;

    public string? RequestPath => httpContextAccessor.HttpContext?.Request.Path.Value;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var claims = httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role);
            return claims?.Select(c => c.Value).ToList() ?? (IReadOnlyCollection<string>)[];
        }
    }

    public bool HasAnyRole(params string[] roles)
    {
        if (roles.Length == 0)
        {
            return true;
        }

        var userRoles = Roles;
        return roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
