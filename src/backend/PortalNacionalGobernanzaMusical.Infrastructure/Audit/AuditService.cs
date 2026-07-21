using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Audit;

public sealed class AuditService(ApplicationDbContext dbContext, ICurrentUserService currentUserService) : IAuditService
{
    public async Task<IReadOnlyCollection<AuditLogDto>> GetLogsAsync(string? entityName = null, Guid? entityId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<AuditLog>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName == entityName);
        if (entityId.HasValue)
            query = query.Where(x => x.EntityId == entityId.Value);

        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return items.Select(MapLog).ToArray();
    }

    public async Task LogAsync(string entityName, Guid entityId, string operation, string? oldValuesJson, string? newValuesJson, CancellationToken cancellationToken = default)
    {
        var email = currentUserService.Email ?? "sistema";
        var displayName = await ResolveDisplayNameAsync(email, cancellationToken);

        dbContext.Set<AuditLog>().Add(new AuditLog
        {
            UserEmail = email,
            UserDisplayName = displayName,
            IpAddress = currentUserService.IpAddress,
            EntityName = entityName,
            EntityId = entityId,
            Operation = operation,
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveDisplayNameAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.ToUpperInvariant();
        var displayName = await dbContext.UserAccounts.AsNoTracking()
            .Where(x => x.NormalizedEmail == normalized)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(displayName) ? email : displayName;
    }

    private static AuditLogDto MapLog(AuditLog x) => new(
        x.Id, x.UserEmail, x.UserDisplayName, x.IpAddress, x.EntityName, x.EntityId, x.Operation, x.OldValuesJson, x.NewValuesJson, x.CreatedAtUtc);
}
