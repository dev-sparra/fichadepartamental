namespace PortalNacionalGobernanzaMusical.Application.Governance;

public sealed record GovernanceFichaSummaryDto(
    Guid Id,
    string DepartmentName,
    DateOnly FechaLevantamiento,
    string ResponsableRegistro,
    string? RegionOcadName,
    string? MunicipalityName,
    int OpportunitiesCount,
    int AxesCount,
    int ActorsCount,
    /// <summary>Estado de revisión: Borrador (sin revisar), Aprobado o Devuelto.</summary>
    string ApprovalStatus);

public sealed record GovernanceFichaDetailDto(
    Guid Id,
    DateOnly FechaLevantamiento,
    int DepartmentId,
    int? MunicipalityId,
    string ResponsableRegistro,
    int? RegionOcadOptionId,
    string? Observaciones,
    IReadOnlyCollection<int> InformationSourceIds);

public sealed record UpdateGovernanceFichaRequest(
    DateOnly FechaLevantamiento,
    int DepartmentId,
    int? MunicipalityId,
    string ResponsableRegistro,
    int? RegionOcadOptionId,
    string? Observaciones,
    IReadOnlyCollection<int> InformationSourceIds);

public sealed record GovernanceDiagnosticDto(
    Guid? Id,
    string? CaracterizacionGeneral,
    string? FortalezasIdentificadas,
    string? PoliticasPriorizadas,
    string? DebilidadesIdentificadas,
    string? TensionesOConflictos,
    int? CommitteeStatusOptionId,
    int? PlanDepartamentalCulturaStatusId,
    string? ConsejoDepartamentalCultura,
    int? PlanDepartamentalMusicaStatusId,
    string? OrdenanzasCulturales,
    string? ConsejoDepartamentalMusica,
    string? MesaSectorialTerritorial,
    string? Observaciones);

public sealed record GovernanceOpportunityDto(
    Guid? Id,
    string? SituacionIdentificada,
    string? ComponenteOtrasDependenciasEntidades,
    string? AliadosYCreyentes,
    string? TerritorioInfluencia,
    int? PriorityLevelOptionId,
    string? DescripcionAdicional);

public sealed record GovernancePnmcAxisDto(
    Guid? Id,
    string? DescripcionHallazgo,
    int? PnmcAxisId,
    int? PnmcComponentId,
    string? AccionEstrategica,
    string? PoliticaPriorizada,
    string? ArmonizacionPnc,
    string? ArmonizacionPnd,
    string? ArmonizacionInternacional,
    int? PriorityLevelOptionId,
    string? AliadosResponsables,
    string? FuentesFinanciacion,
    decimal? ValorPropuestaCop,
    IReadOnlyCollection<int> ApproachOptionIds,
    string? Descripcion,
    int? ScheduleOptionId,
    int? ProposalStatusOptionId,
    string? Observaciones);

public sealed record GovernanceActorDto(
    Guid? Id,
    string NombreAgente,
    int? AgentTypeId,
    IReadOnlyCollection<int> EcosystemRoleIds,
    IReadOnlyCollection<int> TerritorialLevelOptionIds,
    string? NumeroContacto,
    string? CorreoElectronico,
    string? Observaciones);
