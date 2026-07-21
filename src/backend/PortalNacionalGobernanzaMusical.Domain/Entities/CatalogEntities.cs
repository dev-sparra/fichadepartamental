using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class Department : CatalogEntityBase
{
    public ICollection<Municipality> Municipalities { get; set; } = new List<Municipality>();
}

public sealed class Municipality : CatalogEntityBase
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public sealed class RegionOcadOption : CatalogEntityBase;

public sealed class CommitteeStatusOption : CatalogEntityBase;

public sealed class PlanStatusOption : CatalogEntityBase;

public sealed class PriorityLevelOption : CatalogEntityBase;

public sealed class PnmcAxis : CatalogEntityBase
{
    public ICollection<PnmcComponent> Components { get; set; } = new List<PnmcComponent>();
}

public sealed class PnmcComponent : CatalogEntityBase
{
    public int PnmcAxisId { get; set; }
    public PnmcAxis? PnmcAxis { get; set; }
}

public sealed class ApproachOption : CatalogEntityBase;

public sealed class ScheduleOption : CatalogEntityBase;

public sealed class ProposalStatusOption : CatalogEntityBase;

public sealed class AgentType : CatalogEntityBase
{
    public ICollection<EcosystemRole> EcosystemRoles { get; set; } = new List<EcosystemRole>();
}

public sealed class EcosystemRole : CatalogEntityBase
{
    public int AgentTypeId { get; set; }
    public AgentType? AgentType { get; set; }
}

public sealed class TerritorialLevelOption : CatalogEntityBase;

public sealed class InformationSourceOption : CatalogEntityBase;

public sealed class MonthOption : CatalogEntityBase;

public sealed class YearOption
{
    public int Id { get; set; }
    public int Value { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class IndicatorDefinition
{
    public int Id { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IndicatorDetailTemplate> DetailTemplates { get; set; } = new List<IndicatorDetailTemplate>();
}

public sealed class IndicatorDetailTemplate
{
    public int Id { get; set; }
    public int IndicatorDefinitionId { get; set; }
    public IndicatorDefinition? IndicatorDefinition { get; set; }
    public int SortOrder { get; set; }
    public string FormulaLabel { get; set; } = string.Empty;
    public string DetailDescription { get; set; } = string.Empty;
}
