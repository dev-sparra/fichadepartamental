namespace PortalNacionalGobernanzaMusical.Application.Auth;

public interface IAuthenticationService
{
    Task<AuthenticatedUserResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
