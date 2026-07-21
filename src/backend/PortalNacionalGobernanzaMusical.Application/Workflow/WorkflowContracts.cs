namespace PortalNacionalGobernanzaMusical.Application.Workflow;

public sealed record ApprovalActionDto(Guid FichaId, string Action, string? Comment);
public sealed record FichaApprovalStatusDto(Guid FichaId, string Status, string? ReviewedByName, DateTime? ReviewedAtUtc, string? Comment);
public sealed record ApprovalRecordDto(Guid Id, Guid FichaId, string ActorEmail, string ActorName, string Action, string? Comment, DateTime TimestampUtc);

public interface IWorkflowService
{
    Task<FichaApprovalStatusDto> GetStatusAsync(Guid fichaId, CancellationToken cancellationToken = default);
    Task<FichaApprovalStatusDto> ApproveAsync(ApprovalActionDto action, CancellationToken cancellationToken = default);
    Task<FichaApprovalStatusDto> RejectAsync(ApprovalActionDto action, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ApprovalRecordDto>> GetHistoryAsync(Guid fichaId, CancellationToken cancellationToken = default);
}
