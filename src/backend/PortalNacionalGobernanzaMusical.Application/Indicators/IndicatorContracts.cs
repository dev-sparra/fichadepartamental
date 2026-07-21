namespace PortalNacionalGobernanzaMusical.Application.Indicators;

public sealed record IndicatorRecordDto(
    Guid Id,
    int DepartmentId,
    string DepartmentName,
    int IndicatorDefinitionId,
    string IndicatorName,
    string ActionName,
    decimal TargetValue,
    int Year,
    string? Source,
    string? GeneralObservations,
    decimal CurrentValueCalculated,
    decimal CompliancePercentageCalculated,
    IReadOnlyCollection<IndicatorMonthlyProgressDto> MonthlyProgresses);

public sealed record IndicatorMonthlyProgressDto(
    Guid Id,
    int MonthOptionId,
    string MonthName,
    decimal? QuantitativeAdvance,
    string? Detail);

public sealed record UpdateIndicatorMonthlyProgressRequest(
    decimal? QuantitativeAdvance,
    string? Detail);

public sealed record UpdateIndicatorRecordRequest(
    string? Source,
    string? GeneralObservations,
    IReadOnlyCollection<UpdateIndicatorMonthlyProgressRequest> MonthlyProgresses);

public sealed record IndicatorDetailRecordDto(
    Guid Id,
    int DepartmentId,
    string DepartmentName,
    int IndicatorDefinitionId,
    string IndicatorName,
    int IndicatorDetailTemplateId,
    string FormulaLabel,
    string DetailDescription,
    int Year,
    string? Source,
    string? Observations,
    decimal? CurrentValueCalculated,
    IReadOnlyCollection<int> SelectedMonthIds);

public sealed record UpdateIndicatorDetailRecordRequest(
    string? Source,
    string? Observations,
    IReadOnlyCollection<int> SelectedMonthIds);

public sealed record ProvisionWorksheetRequest(int DepartmentId, int Year);

/// <summary>
/// Hoja de trabajo de indicadores para un departamento y año: los 7 indicadores fijos y sus
/// detalles, ya materializados (creados desde el catálogo si no existían) y listos para diligenciar.
/// </summary>
public sealed record IndicatorWorksheetDto(
    IReadOnlyCollection<IndicatorRecordDto> Records,
    IReadOnlyCollection<IndicatorDetailRecordDto> Details);
