namespace PortalNacionalGobernanzaMusical.Application.Imports;

public interface ICatalogLookupService
{
    Task<CatalogValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record CatalogValidationSnapshot(
    HashSet<string> Departments,
    Dictionary<string, HashSet<string>> MunicipalitiesByDepartment,
    HashSet<string> RegionOcad,
    HashSet<string> InformationSources,
    HashSet<string> CommitteeStatuses,
    HashSet<string> PlanStatuses,
    HashSet<string> PriorityLevels,
    HashSet<string> PnmcAxes,
    Dictionary<string, HashSet<string>> ComponentsByAxis,
    HashSet<string> Approaches,
    HashSet<string> ScheduleOptions,
    HashSet<string> ProposalStatuses,
    HashSet<string> AgentTypes,
    Dictionary<string, HashSet<string>> EcosystemRolesByAgentType,
    HashSet<string> TerritorialLevels,
    HashSet<string> Months,
    HashSet<int> Years,
    HashSet<string> IndicatorNames);
