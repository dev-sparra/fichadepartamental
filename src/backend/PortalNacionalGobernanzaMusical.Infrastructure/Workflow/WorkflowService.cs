using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Notifications;
using PortalNacionalGobernanzaMusical.Application.Workflow;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Workflow;

public sealed class WorkflowService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationService notificationService) : IWorkflowService
{
    private const string StatusAprobado = "Aprobado";
    private const string StatusDevuelto = "Devuelto";

    /// <summary>Nombre del rol tal como está registrado en <c>security_roles</c>.</summary>
    private const string RoleGestorDepartamental = "Gestor Departamental";

    /// <summary>Ruta del portal a la que lleva el aviso de cambio de estado.</summary>
    private const string GovernanceRoute = "/governance";

    public async Task<FichaApprovalStatusDto> GetStatusAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Set<ApprovalRecord>().AsNoTracking()
            .Where(x => x.FichaDepartamentalId == fichaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null
            ? new FichaApprovalStatusDto(fichaId, "Borrador", null, null, null)
            : new FichaApprovalStatusDto(fichaId, record.Status, record.ReviewedByName ?? record.ReviewedByEmail, record.ReviewedAtUtc, record.Comment);
    }

    public async Task<FichaApprovalStatusDto> ApproveAsync(ApprovalActionDto action, CancellationToken cancellationToken = default)
    {
        return await RecordAction(action.FichaId, StatusAprobado, action.Comment, cancellationToken);
    }

    public async Task<FichaApprovalStatusDto> RejectAsync(ApprovalActionDto action, CancellationToken cancellationToken = default)
    {
        return await RecordAction(action.FichaId, StatusDevuelto, action.Comment, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ApprovalRecordDto>> GetHistoryAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Set<ApprovalRecord>().AsNoTracking()
            .Where(x => x.FichaDepartamentalId == fichaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return records.Select(x => new ApprovalRecordDto(
            x.Id, x.FichaDepartamentalId, x.ReviewedByEmail ?? "sistema", x.ReviewedByName ?? x.ReviewedByEmail ?? "Sistema", x.Status, x.Comment, x.CreatedAtUtc)).ToArray();
    }

    private async Task<FichaApprovalStatusDto> RecordAction(Guid fichaId, string status, string? comment, CancellationToken cancellationToken)
    {
        var email = currentUserService.Email ?? "sistema";
        var displayName = await ResolveDisplayNameAsync(email, cancellationToken);

        var record = new ApprovalRecord
        {
            FichaDepartamentalId = fichaId,
            Status = status,
            ReviewedByEmail = email,
            ReviewedByName = displayName,
            ReviewedAtUtc = DateTime.UtcNow,
            Comment = comment
        };

        dbContext.Set<ApprovalRecord>().Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        await NotifyGestorAsync(fichaId, status, comment, displayName, cancellationToken);

        return new FichaApprovalStatusDto(fichaId, status, record.ReviewedByName, record.ReviewedAtUtc, record.Comment);
    }

    /// <summary>
    /// Avisa al Gestor Departamental del cambio de estado de su ficha. El destinatario natural es
    /// quien la creó; si la ficha no tiene autor registrado (por ejemplo, cargas antiguas) se avisa
    /// a los gestores activos para que el cambio no pase inadvertido.
    /// </summary>
    private async Task NotifyGestorAsync(
        Guid fichaId,
        string status,
        string? comment,
        string reviewerName,
        CancellationToken cancellationToken)
    {
        var ficha = await dbContext.FichasDepartamentales.AsNoTracking()
            .Where(x => x.Id == fichaId)
            .Select(x => new { x.CreatedByEmail, DepartmentName = x.Department!.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (ficha is null)
        {
            return;
        }

        var approved = status == StatusAprobado;
        var motivo = string.IsNullOrWhiteSpace(comment) ? null : $" Observación del revisor: {comment.Trim()}";

        var title = approved ? "Ficha aprobada" : "Ficha devuelta para ajustes";
        var message = approved
            ? $"{reviewerName} aprobó la ficha departamental de {ficha.DepartmentName}. La información queda en firme y disponible en el módulo de Gobernanza.{motivo}"
            : $"{reviewerName} devolvió la ficha departamental de {ficha.DepartmentName} para ajustes. Realiza las correcciones indicadas y guarda de nuevo cada sección.{motivo}";

        var recipients = await ResolveRecipientsAsync(ficha.CreatedByEmail, cancellationToken);

        foreach (var recipient in recipients)
        {
            await notificationService.NotifyAsync(new CreateNotificationCommand(
                recipient,
                NotificationCategories.Gobernanza,
                approved ? NotificationEvents.FichaAprobada : NotificationEvents.FichaDevuelta,
                title,
                message,
                approved ? NotificationTones.Success : NotificationTones.Warning,
                GovernanceRoute,
                nameof(FichaDepartamental),
                fichaId), cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<string>> ResolveRecipientsAsync(string? createdByEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(createdByEmail))
        {
            return [createdByEmail];
        }

        return await dbContext.UserAccounts.AsNoTracking()
            .Where(user => user.IsActive
                && user.RoleAssignments.Any(assignment => assignment.Role!.Name == RoleGestorDepartamental))
            .Select(user => user.Email)
            .ToArrayAsync(cancellationToken);
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
}
