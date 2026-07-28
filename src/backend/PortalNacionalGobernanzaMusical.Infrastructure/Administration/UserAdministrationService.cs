using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Administration;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Administration;

public sealed class UserAdministrationService(
    ApplicationDbContext dbContext,
    IAuditService auditService) : IUserAdministrationService
{
    private const string EntityName = nameof(UserAccount);

    private readonly PasswordHasher<UserAccount> hasher = new();

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.UserAccounts.AsNoTracking()
            .Include(x => x.RoleAssignments).ThenInclude(x => x.Role)
            .OrderBy(x => x.Email)
            .ToArrayAsync(cancellationToken);

        return users.Select(MapUser).ToArray();
    }

    public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.UserAccounts.AsNoTracking()
            .Include(x => x.RoleAssignments).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return user is null ? null : MapUser(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = request.Email.Trim().ToUpperInvariant();
        if (await dbContext.UserAccounts.AnyAsync(x => x.NormalizedEmail == normalized, cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");

        var user = new UserAccount
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            DisplayName = request.DisplayName,
            PasswordHash = hasher.HashPassword(null!, request.Password),
            IsActive = true,
            MustChangePassword = true
        };

        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncRoles(user.Id, request.RoleNames, cancellationToken);

        var created = MapUser(user);
        var roles = string.Join(", ", request.RoleNames);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Seguridad,
            EntityName = EntityName,
            EntityId = user.Id,
            EntityLabel = DescribeUser(user.DisplayName, user.Email),
            Operation = "Crear usuario",
            Description = $"Creó el usuario \"{user.Email}\" con el rol {roles}. Deberá cambiar la contraseña en su primer ingreso.",
            Changes = new AuditChangeSet()
                .Track("email", "Correo", null, user.Email)
                .Track("displayName", "Nombre", null, user.DisplayName)
                .Track("roles", "Roles", null, request.RoleNames)
                .Track("isActive", "Activo", null, user.IsActive)
                .Changes,
            NewValuesJson = JsonSerializer.Serialize(created)
        }, cancellationToken);

        return created;
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var before = await dbContext.UserAccounts.AsNoTracking()
            .Include(x => x.RoleAssignments).ThenInclude(x => x.Role)
            .SingleAsync(x => x.Id == id, cancellationToken);
        var beforeDto = MapUser(before);

        var user = await dbContext.UserAccounts
            .Include(x => x.RoleAssignments)
            .SingleAsync(x => x.Id == id, cancellationToken);

        user.Email = request.Email.Trim();
        user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;

        await SyncRoles(user.Id, request.RoleNames, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        user = await dbContext.UserAccounts.AsNoTracking()
            .Include(x => x.RoleAssignments).ThenInclude(x => x.Role)
            .SingleAsync(x => x.Id == id, cancellationToken);
        var afterDto = MapUser(user);

        var changes = new AuditChangeSet()
            .Track("email", "Correo", beforeDto.Email, afterDto.Email)
            .Track("displayName", "Nombre", beforeDto.DisplayName, afterDto.DisplayName)
            .Track("isActive", "Activo", beforeDto.IsActive, afterDto.IsActive)
            .Track("roles", "Roles", beforeDto.Roles, afterDto.Roles);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Seguridad,
            EntityName = EntityName,
            EntityId = user.Id,
            EntityLabel = DescribeUser(user.DisplayName, user.Email),
            Operation = "Actualizar usuario",
            Description = changes.HasChanges
                ? $"Modificó {changes.DescribeChangedFields()} del usuario \"{user.Email}\"."
                : $"Guardó el usuario \"{user.Email}\" sin cambios.",
            Changes = changes.Changes,
            OldValuesJson = JsonSerializer.Serialize(beforeDto),
            NewValuesJson = JsonSerializer.Serialize(afterDto)
        }, cancellationToken);

        return afterDto;
    }

    public async Task<ResetPasswordResult?> ResetUserPasswordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.UserAccounts
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
            return null;

        if (!user.IsActive)
            throw new InvalidOperationException("No se puede restablecer la contraseña de un usuario inactivo.");

        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Seguridad,
            EntityName = EntityName,
            EntityId = user.Id,
            EntityLabel = DescribeUser(user.DisplayName, user.Email),
            Operation = "Restablecer contraseña",
            Description = $"Restableció la contraseña del usuario \"{user.Email}\" y generó una temporal. El usuario deberá cambiarla en su próximo ingreso.",
            // La contraseña temporal no se guarda en el historial: se entrega en pantalla una sola vez.
            Changes = new AuditChangeSet()
                .TrackSecret("password", "Contraseña")
                .Track("mustChangePassword", "Debe cambiar la contraseña", false, true)
                .Changes
        }, cancellationToken);

        return new ResetPasswordResult(user.Id, user.Email, temporaryPassword);
    }

    private static string GenerateTemporaryPassword(int length = 16)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*";
        var all = $"{upper}{lower}{digits}{symbols}";

        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = all[bytes[i] % all.Length];

        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = symbols[bytes[3] % symbols.Length];

        return new string(chars);
    }

    private async Task SyncRoles(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        dbContext.UserRoleAssignments.RemoveRange(
            dbContext.UserRoleAssignments.Where(x => x.UserAccountId == userId));

        foreach (var name in roleNames.Distinct())
        {
            var role = await dbContext.Roles.SingleAsync(x => x.NormalizedName == name.ToUpperInvariant(), cancellationToken);
            dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserAccountId = userId, RoleId = role.Id });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string DescribeUser(string? displayName, string email) =>
        string.IsNullOrWhiteSpace(displayName) ? email : $"{displayName} ({email})";

    private static UserDto MapUser(UserAccount x) => new(
        x.Id, x.Email, x.DisplayName, x.IsActive,
        x.RoleAssignments.Select(r => r.Role?.Name ?? string.Empty).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray());
}
