/** Tono de presentación que envía el backend para estados e incidencias. */
export type ImportTone = 'success' | 'warning' | 'error' | 'progress' | 'info';

export interface ImportBatchSummary {
  importBatchId: string;
  fileName: string;
  /** Código interno del estado (no se muestra al usuario). */
  status: string;
  statusLabel: string;
  statusDescription: string;
  statusNextStep: string;
  statusTone: ImportTone;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  persistedRecordCount: number;
  startedAtUtc: string;
  completedAtUtc: string | null;
}

/**
 * Incidencia ya redactada por el backend en lenguaje funcional: ubica el dato y explica qué
 * corregir. `technicalDetail` es información de soporte y no se muestra como mensaje principal.
 */
export interface ImportValidationIssue {
  id: string;
  severity: 'Error' | 'Warning' | 'Info' | string;
  severityLabel: string;
  sheetName: string;
  rowNumber: number | null;
  cellReference: string | null;
  columnLetter: string | null;
  fieldLabel: string | null;
  errorCode: string;
  title: string;
  message: string;
  rawValue: string | null;
  expectedValue: string | null;
  howToFix: string | null;
  technicalDetail: string | null;
}

export interface ImportWorkbookResult {
  importBatchId: string;
  status: string;
  statusLabel: string;
  statusDescription: string;
  statusNextStep: string;
  statusTone: ImportTone;
  /** `false` cuando el archivo se rechazó: no se importó ningún dato. */
  accepted: boolean;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  persistedRecordCount: number;
  issues: ImportValidationIssue[];
}
