using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Workflow;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Workflow;

public sealed class WorkflowService(ApplicationDbContext dbContext, ICurrentUserService currentUserService) : IWorkflowService
{
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
        return await RecordAction(action.FichaId, "Aprobado", action.Comment, cancellationToken);
    }

    public async Task<FichaApprovalStatusDto> RejectAsync(ApprovalActionDto action, CancellationToken cancellationToken = default)
    {
        return await RecordAction(action.FichaId, "Devuelto", action.Comment, cancellationToken);
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

        return new FichaApprovalStatusDto(fichaId, status, record.ReviewedByName, record.ReviewedAtUtc, record.Comment);
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
