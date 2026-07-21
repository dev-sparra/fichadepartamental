namespace PortalNacionalGobernanzaMusical.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthenticatedUserResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Email,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    bool MustChangePassword);
