namespace PortalNacionalGobernanzaMusical.Application.Imports;

public sealed record ImportWorkbookCommand(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream);

/// <summary>
/// Resultado de una carga. <paramref name="Accepted"/> indica si el archivo superó la validación
/// de formato y estructura; cuando es <c>false</c> no se importó ningún dato.
/// </summary>
public sealed record ImportWorkbookResult(
    Guid ImportBatchId,
    string Status,
    string StatusLabel,
    string StatusDescription,
    string StatusNextStep,
    string StatusTone,
    bool Accepted,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    int PersistedRecordCount,
    IReadOnlyCollection<ImportValidationIssueDto> Issues);

public sealed record ImportBatchSummaryDto(
    Guid ImportBatchId,
    string FileName,
    string Status,
    string StatusLabel,
    string StatusDescription,
    string StatusNextStep,
    string StatusTone,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    int PersistedRecordCount,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc);

/// <summary>
/// Incidencia redactada en lenguaje funcional: ubica el dato (hoja, fila, columna, campo),
/// muestra el valor recibido, el valor esperado y la acción concreta de corrección.
/// <para><paramref name="TechnicalDetail"/> es información de diagnóstico para soporte y no
/// debe presentarse como mensaje principal al usuario final.</para>
/// </summary>
public sealed record ImportValidationIssueDto(
    Guid Id,
    string Severity,
    string SeverityLabel,
    string SheetName,
    int? RowNumber,
    string? CellReference,
    string? ColumnLetter,
    string? FieldLabel,
    string ErrorCode,
    string Title,
    string Message,
    string? RawValue,
    string? ExpectedValue,
    string? HowToFix,
    string? TechnicalDetail);
