namespace PortalNacionalGobernanzaMusical.Application.Administration;

public interface IUserAdministrationService
{
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ResetPasswordResult?> ResetUserPasswordAsync(Guid id, CancellationToken cancellationToken = default);
}
