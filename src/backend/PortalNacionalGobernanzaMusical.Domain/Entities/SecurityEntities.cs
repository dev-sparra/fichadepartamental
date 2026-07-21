using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class UserAccount : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }

    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = new List<UserRoleAssignment>();
}

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<UserRoleAssignment> UserAssignments { get; set; } = new List<UserRoleAssignment>();
}

public sealed class UserRoleAssignment
{
    public Guid UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}
