namespace PortalNacionalGobernanzaMusical.Application.Imports;

public sealed record ImportWorkbookCommand(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream);

public sealed record ImportWorkbookResult(
    Guid ImportBatchId,
    string Status,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    IReadOnlyCollection<ImportValidationIssueDto> Issues);

public sealed record ImportBatchSummaryDto(
    Guid ImportBatchId,
    string FileName,
    string Status,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record ImportValidationIssueDto(
    Guid Id,
    string Severity,
    string SheetName,
    int? RowNumber,
    string? CellReference,
    string ErrorCode,
    string Message,
    string? RawValue);
