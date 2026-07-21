namespace PortalNacionalGobernanzaMusical.Application.Administration;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string? DisplayName,
    IReadOnlyCollection<string> RoleNames);

public sealed record UpdateUserRequest(
    string Email,
    string? DisplayName,
    bool IsActive,
    IReadOnlyCollection<string> RoleNames);

/// <summary>Resultado de restablecer la contraseña de un usuario. Contiene la contraseña temporal generada para que el administrador la comparta por canal seguro.</summary>
public sealed record ResetPasswordResult(
    Guid UserId,
    string Email,
    string TemporaryPassword);
