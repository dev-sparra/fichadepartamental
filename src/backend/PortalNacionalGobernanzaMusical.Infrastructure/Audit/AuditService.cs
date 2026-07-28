using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Audit;

public sealed class AuditService(ApplicationDbContext dbContext, ICurrentUserService currentUserService) : IAuditService
{
    /// <summary>Tope de resultados por página, para que una consulta no arrastre el historial entero.</summary>
    private const int MaxPageSize = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuditLogPageDto> GetLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var filtered = ApplyFilters(dbContext.Set<AuditLog>().AsNoTracking(), query);

        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new AuditLogPageDto(items.Select(MapLog).ToArray(), total, page, pageSize);
    }

    public async Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var logs = dbContext.Set<AuditLog>().AsNoTracking();

        var modules = await logs.Select(x => x.Module).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
        var operations = await logs.Select(x => x.Operation).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
        // Un mismo correo puede aparecer con nombres distintos si el usuario se renombró; se
        // resuelve la pareja correo/nombre en la base y se elige el más reciente aquí, sobre una
        // lista que tiene como mucho una entrada por nombre que haya usado cada persona.
        var namesUsed = await logs
            .GroupBy(x => new { x.UserEmail, x.UserDisplayName })
            .Select(group => new
            {
                group.Key.UserEmail,
                group.Key.UserDisplayName,
                LastUsedAtUtc = group.Max(x => x.CreatedAtUtc)
            })
            .ToArrayAsync(cancellationToken);

        var users = namesUsed
            .GroupBy(x => x.UserEmail)
            .Select(group => group.OrderByDescending(x => x.LastUsedAtUtc).First())
            .Select(x => new AuditUserOptionDto(x.UserEmail, x.UserDisplayName))
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new AuditFilterOptionsDto(
            modules.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            operations.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            users);
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var email = entry.UserEmailOverride ?? currentUserService.Email ?? "sistema";
        var displayName = await ResolveDisplayNameAsync(email, cancellationToken);
        var roles = currentUserService.Roles;

        dbContext.Set<AuditLog>().Add(new AuditLog
        {
            UserEmail = email,
            UserDisplayName = displayName,
            UserRoles = roles.Count > 0 ? string.Join(", ", roles) : null,
            IpAddress = currentUserService.IpAddress,
            Module = entry.Module,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            EntityKey = entry.EntityKey,
            EntityLabel = entry.EntityLabel,
            Operation = entry.Operation,
            Description = entry.Description,
            Result = entry.Result,
            ChangesJson = entry.Changes.Count > 0 ? JsonSerializer.Serialize(entry.Changes, JsonOptions) : null,
            RequestMethod = currentUserService.RequestMethod,
            RequestPath = currentUserService.RequestPath,
            OldValuesJson = entry.OldValuesJson,
            NewValuesJson = entry.NewValuesJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Module))
            query = query.Where(x => x.Module == filters.Module);

        if (!string.IsNullOrWhiteSpace(filters.UserEmail))
            query = query.Where(x => x.UserEmail == filters.UserEmail);

        if (!string.IsNullOrWhiteSpace(filters.Operation))
            query = query.Where(x => x.Operation == filters.Operation);

        if (!string.IsNullOrWhiteSpace(filters.EntityName))
            query = query.Where(x => x.EntityName == filters.EntityName);

        if (filters.EntityId.HasValue)
            query = query.Where(x => x.EntityId == filters.EntityId.Value);

        if (!string.IsNullOrWhiteSpace(filters.Result))
            query = query.Where(x => x.Result == filters.Result);

        if (filters.FromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= filters.FromUtc.Value);

        if (filters.ToUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= filters.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            // Búsqueda libre sobre lo que se ve en pantalla: quién, sobre qué, qué hizo.
            var term = filters.Search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.UserDisplayName, $"%{term}%")
                || EF.Functions.Like(x.UserEmail, $"%{term}%")
                || EF.Functions.Like(x.Operation, $"%{term}%")
                || (x.EntityLabel != null && EF.Functions.Like(x.EntityLabel, $"%{term}%"))
                || (x.Description != null && EF.Functions.Like(x.Description, $"%{term}%")));
        }

        return query;
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
        x.Id,
        x.UserEmail,
        x.UserDisplayName,
        x.UserRoles,
        x.IpAddress,
        // Los registros anteriores al historial detallado no traen módulo.
        string.IsNullOrWhiteSpace(x.Module) ? "Sin clasificar" : x.Module,
        x.EntityName,
        x.EntityId,
        x.EntityKey,
        x.EntityLabel,
        x.Operation,
        x.Description,
        string.IsNullOrWhiteSpace(x.Result) ? AuditResults.Exitoso : x.Result,
        DeserializeChanges(x.ChangesJson),
        x.RequestMethod,
        x.RequestPath,
        x.OldValuesJson,
        x.NewValuesJson,
        x.CreatedAtUtc);

    private static IReadOnlyCollection<AuditChangeDto> DeserializeChanges(string? changesJson)
    {
        if (string.IsNullOrWhiteSpace(changesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<AuditChangeDto[]>(changesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            // Un registro con el detalle corrupto no debe tumbar la consulta del historial.
            return [];
        }
    }
}
