export interface ImportBatchSummary {
  importBatchId: string;
  fileName: string;
  status: string;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  startedAtUtc: string;
  completedAtUtc: string | null;
}

export interface ImportValidationIssue {
  id: string;
  severity: string;
  sheetName: string;
  rowNumber: number | null;
  cellReference: string | null;
  errorCode: string;
  message: string;
  rawValue: string | null;
}

export interface ImportWorkbookResult {
  importBatchId: string;
  status: string;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  issues: ImportValidationIssue[];
}
