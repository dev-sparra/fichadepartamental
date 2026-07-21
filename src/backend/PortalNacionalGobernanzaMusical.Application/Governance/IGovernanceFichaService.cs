namespace PortalNacionalGobernanzaMusical.Application.Governance;

public interface IGovernanceFichaService
{
    Task<IReadOnlyCollection<GovernanceFichaSummaryDto>> GetFichasAsync(CancellationToken cancellationToken = default);
    Task<GovernanceFichaDetailDto?> GetFichaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GovernanceFichaDetailDto> CreateFichaAsync(UpdateGovernanceFichaRequest request, CancellationToken cancellationToken = default);
    Task<GovernanceFichaDetailDto> UpdateFichaAsync(Guid id, UpdateGovernanceFichaRequest request, CancellationToken cancellationToken = default);
    Task DeleteFichaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GovernanceDiagnosticDto?> GetDiagnosticAsync(Guid fichaId, CancellationToken cancellationToken = default);
    Task<GovernanceDiagnosticDto> UpdateDiagnosticAsync(Guid fichaId, GovernanceDiagnosticDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernanceOpportunityDto>> GetOpportunitiesAsync(Guid fichaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernanceOpportunityDto>> ReplaceOpportunitiesAsync(Guid fichaId, IReadOnlyCollection<GovernanceOpportunityDto> request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernancePnmcAxisDto>> GetPnmcAxesAsync(Guid fichaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernancePnmcAxisDto>> ReplacePnmcAxesAsync(Guid fichaId, IReadOnlyCollection<GovernancePnmcAxisDto> request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernanceActorDto>> GetActorsAsync(Guid fichaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GovernanceActorDto>> ReplaceActorsAsync(Guid fichaId, IReadOnlyCollection<GovernanceActorDto> request, CancellationToken cancellationToken = default);
}
