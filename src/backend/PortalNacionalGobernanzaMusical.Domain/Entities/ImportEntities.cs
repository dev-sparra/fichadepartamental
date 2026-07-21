using PortalNacionalGobernanzaMusical.Domain.Common;

namespace PortalNacionalGobernanzaMusical.Domain.Entities;

public sealed class ImportBatch : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Excel";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int ValidRowCount { get; set; }
    public int InvalidRowCount { get; set; }
    public int WarningCount { get; set; }
    public int PersistedRecordCount { get; set; }
    public string? SummaryJson { get; set; }

    public ICollection<ImportValidationIssue> ValidationIssues { get; set; } = new List<ImportValidationIssue>();
}

public sealed class ImportValidationIssue : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int? RowNumber { get; set; }
    public string? CellReference { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? ContextJson { get; set; }
}

public sealed class ImportIdentificationStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public DateOnly? FechaLevantamiento { get; set; }
    public string? DepartmentName { get; set; }
    public string? MunicipalityName { get; set; }
    public string? ResponsableRegistro { get; set; }
    public string? RegionOcadName { get; set; }
    public string? InformationSourcesRaw { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ImportDiagnosticStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? CaracterizacionGeneral { get; set; }
    public string? FortalezasIdentificadas { get; set; }
    public string? PoliticasPriorizadas { get; set; }
    public string? DebilidadesIdentificadas { get; set; }
    public string? TensionesOConflictos { get; set; }
    public string? CommitteeStatusName { get; set; }
    public string? PlanDepartamentalCulturaStatusName { get; set; }
    public string? ConsejoDepartamentalCultura { get; set; }
    public string? PlanDepartamentalMusicaStatusName { get; set; }
    public string? OrdenanzasCulturales { get; set; }
    public string? ConsejoDepartamentalMusica { get; set; }
    public string? MesaSectorialTerritorial { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ImportOpportunityStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? SituacionIdentificada { get; set; }
    public string? ComponenteOtrasDependenciasEntidades { get; set; }
    public string? AliadosYCreyentes { get; set; }
    public string? TerritorioInfluencia { get; set; }
    public string? PriorityLevelName { get; set; }
    public string? DescripcionAdicional { get; set; }
}

public sealed class ImportPnmcAxisStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? DescripcionHallazgo { get; set; }
    public string? PnmcAxisName { get; set; }
    public string? PnmcComponentName { get; set; }
    public string? AccionEstrategica { get; set; }
    public string? PoliticaPriorizada { get; set; }
    public string? ArmonizacionPnc { get; set; }
    public string? ArmonizacionPnd { get; set; }
    public string? ArmonizacionInternacional { get; set; }
    public string? PriorityLevelName { get; set; }
    public string? AliadosResponsables { get; set; }
    public string? FuentesFinanciacion { get; set; }
    public decimal? ValorPropuestaCop { get; set; }
    public string? EnfoquesRaw { get; set; }
    public string? Descripcion { get; set; }
    public string? ScheduleName { get; set; }
    public string? ProposalStatusName { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ImportActorStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? NombreAgente { get; set; }
    public string? AgentTypeName { get; set; }
    public string? EcosystemRolesRaw { get; set; }
    public string? TerritorialLevelsRaw { get; set; }
    public string? NumeroContacto { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ImportIndicatorStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? DepartmentsRaw { get; set; }
    public string? ActionName { get; set; }
    public string? IndicatorName { get; set; }
    public string? TargetRaw { get; set; }
    public string? MonthlyQuantitativeJson { get; set; }
    public string? MonthlyDetailJson { get; set; }
    public string? Fuente { get; set; }
    public string? YearRaw { get; set; }
    public string? ObservacionesGenerales { get; set; }
}

public sealed class ImportIndicatorDetailStagingRow : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string? DepartmentsRaw { get; set; }
    public string? ActionName { get; set; }
    public string? IndicatorName { get; set; }
    public string? TargetRaw { get; set; }
    public string? FormulaLabel { get; set; }
    public string? DetailDescription { get; set; }
    public string? MonthsRaw { get; set; }
    public string? CurrentValueRaw { get; set; }
    public string? Fuente { get; set; }
    public string? YearRaw { get; set; }
    public string? Observaciones { get; set; }
}
