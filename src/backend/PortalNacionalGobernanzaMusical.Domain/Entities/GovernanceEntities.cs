using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class FichaDepartamental : BaseEntity
{
    public DateOnly FechaLevantamiento { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
    public string ResponsableRegistro { get; set; } = string.Empty;
    public int? RegionOcadOptionId { get; set; }
    public RegionOcadOption? RegionOcadOption { get; set; }
    public string? Observaciones { get; set; }
    public string? CreatedByEmail { get; set; }

    public DiagnosticoEcosistema? DiagnosticoEcosistema { get; set; }
    public ICollection<FichaFuenteInformacion> FuentesInformacion { get; set; } = new List<FichaFuenteInformacion>();
    public ICollection<OportunidadCambio> OportunidadesCambio { get; set; } = new List<OportunidadCambio>();
    public ICollection<EjePnmcRegistro> EjesPnmc { get; set; } = new List<EjePnmcRegistro>();
    public ICollection<Actor> Actores { get; set; } = new List<Actor>();
}

public sealed class FichaFuenteInformacion
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }

    public int InformationSourceOptionId { get; set; }
    public InformationSourceOption? InformationSourceOption { get; set; }
}

public sealed class DiagnosticoEcosistema : BaseEntity
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }
    public string? CaracterizacionGeneral { get; set; }
    public string? FortalezasIdentificadas { get; set; }
    public string? PoliticasPriorizadas { get; set; }
    public string? DebilidadesIdentificadas { get; set; }
    public string? TensionesOConflictos { get; set; }
    public int? CommitteeStatusOptionId { get; set; }
    public CommitteeStatusOption? CommitteeStatusOption { get; set; }
    public int? PlanDepartamentalCulturaStatusId { get; set; }
    public PlanStatusOption? PlanDepartamentalCulturaStatus { get; set; }
    public string? ConsejoDepartamentalCultura { get; set; }
    public int? PlanDepartamentalMusicaStatusId { get; set; }
    public PlanStatusOption? PlanDepartamentalMusicaStatus { get; set; }
    public string? OrdenanzasCulturales { get; set; }
    public string? ConsejoDepartamentalMusica { get; set; }
    public string? MesaSectorialTerritorial { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class OportunidadCambio : BaseEntity
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }
    public string? SituacionIdentificada { get; set; }
    public string? ComponenteOtrasDependenciasEntidades { get; set; }
    public string? AliadosYCreyentes { get; set; }
    public string? TerritorioInfluencia { get; set; }
    public int? PriorityLevelOptionId { get; set; }
    public PriorityLevelOption? PriorityLevelOption { get; set; }
    public string? DescripcionAdicional { get; set; }
}

public sealed class EjePnmcRegistro : BaseEntity
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }
    public string? DescripcionHallazgo { get; set; }
    public int? PnmcAxisId { get; set; }
    public PnmcAxis? PnmcAxis { get; set; }
    public int? PnmcComponentId { get; set; }
    public PnmcComponent? PnmcComponent { get; set; }
    public string? AccionEstrategica { get; set; }
    public string? PoliticaPriorizada { get; set; }
    public string? ArmonizacionPnc { get; set; }
    public string? ArmonizacionPnd { get; set; }
    public string? ArmonizacionInternacional { get; set; }
    public int? PriorityLevelOptionId { get; set; }
    public PriorityLevelOption? PriorityLevelOption { get; set; }
    public string? AliadosResponsables { get; set; }
    public string? FuentesFinanciacion { get; set; }
    public decimal? ValorPropuestaCop { get; set; }
    public string? Descripcion { get; set; }
    public int? ScheduleOptionId { get; set; }
    public ScheduleOption? ScheduleOption { get; set; }
    public int? ProposalStatusOptionId { get; set; }
    public ProposalStatusOption? ProposalStatusOption { get; set; }
    public string? Observaciones { get; set; }

    public ICollection<EjePnmcRegistroEnfoque> Enfoques { get; set; } = new List<EjePnmcRegistroEnfoque>();
}

public sealed class EjePnmcRegistroEnfoque
{
    public Guid EjePnmcRegistroId { get; set; }
    public EjePnmcRegistro? EjePnmcRegistro { get; set; }

    public int ApproachOptionId { get; set; }
    public ApproachOption? ApproachOption { get; set; }
}

public sealed class Actor : BaseEntity
{
    public Guid FichaDepartamentalId { get; set; }
    public FichaDepartamental? FichaDepartamental { get; set; }
    public string NombreAgente { get; set; } = string.Empty;
    public int? AgentTypeId { get; set; }
    public AgentType? AgentType { get; set; }
    public string? NumeroContacto { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? Observaciones { get; set; }

    public ICollection<ActorRolEcosistema> RolesEcosistema { get; set; } = new List<ActorRolEcosistema>();
    public ICollection<ActorNivelTerritorial> NivelesTerritoriales { get; set; } = new List<ActorNivelTerritorial>();
}

public sealed class ActorRolEcosistema
{
    public Guid ActorId { get; set; }
    public Actor? Actor { get; set; }

    public int EcosystemRoleId { get; set; }
    public EcosystemRole? EcosystemRole { get; set; }
}

public sealed class ActorNivelTerritorial
{
    public Guid ActorId { get; set; }
    public Actor? Actor { get; set; }

    public int TerritorialLevelOptionId { get; set; }
    public TerritorialLevelOption? TerritorialLevelOption { get; set; }
}
