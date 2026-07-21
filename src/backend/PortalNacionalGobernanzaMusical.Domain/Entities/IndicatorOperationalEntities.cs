using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class IndicatorRecord : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int IndicatorDefinitionId { get; set; }
    public IndicatorDefinition? IndicatorDefinition { get; set; }
    public int Year { get; set; }
    public string? Source { get; set; }
    public string? GeneralObservations { get; set; }
    public decimal CurrentValueCalculated { get; set; }
    public decimal CompliancePercentageCalculated { get; set; }

    public ICollection<IndicatorMonthlyProgress> MonthlyProgresses { get; set; } = new List<IndicatorMonthlyProgress>();
}

public sealed class IndicatorMonthlyProgress : BaseEntity
{
    public Guid IndicatorRecordId { get; set; }
    public IndicatorRecord? IndicatorRecord { get; set; }
    public int MonthOptionId { get; set; }
    public MonthOption? MonthOption { get; set; }
    public decimal? QuantitativeAdvance { get; set; }
    public string? Detail { get; set; }
}

public sealed class IndicatorDetailRecord : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int IndicatorDefinitionId { get; set; }
    public IndicatorDefinition? IndicatorDefinition { get; set; }
    public int IndicatorDetailTemplateId { get; set; }
    public IndicatorDetailTemplate? IndicatorDetailTemplate { get; set; }
    public int Year { get; set; }
    public string? Source { get; set; }
    public string? Observations { get; set; }
    public decimal? CurrentValueCalculated { get; set; }

    public ICollection<IndicatorDetailRecordMonth> SelectedMonths { get; set; } = new List<IndicatorDetailRecordMonth>();
}

public sealed class IndicatorDetailRecordMonth : BaseEntity
{
    public Guid IndicatorDetailRecordId { get; set; }
    public IndicatorDetailRecord? IndicatorDetailRecord { get; set; }
    public int MonthOptionId { get; set; }
    public MonthOption? MonthOption { get; set; }
}
