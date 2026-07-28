/** Un campo que cambió en una acción, con su valor anterior y el nuevo. */
export interface AuditChange {
  field: string;
  label: string;
  before: string | null;
  after: string | null;
}

export interface AuditLog {
  id: string;
  userEmail: string;
  userDisplayName: string;
  userRoles: string | null;
  ipAddress: string | null;
  module: string;
  entityName: string;
  entityId: string | null;
  entityKey: string | null;
  /** Objeto afectado en palabras: "ficha de Antioquia · 15/03/2026". */
  entityLabel: string | null;
  operation: string;
  /** Qué ocurrió, redactado para una persona. */
  description: string | null;
  result: 'Exitoso' | 'Fallido' | string;
  changes: AuditChange[];
  requestMethod: string | null;
  requestPath: string | null;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  timestampUtc: string;
}

export interface AuditLogPage {
  items: AuditLog[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AuditUserOption {
  email: string;
  displayName: string;
}

export interface AuditFilterOptions {
  modules: string[];
  operations: string[];
  users: AuditUserOption[];
}

export interface AuditLogQuery {
  module?: string | null;
  userEmail?: string | null;
  operation?: string | null;
  entityName?: string | null;
  entityId?: string | null;
  result?: string | null;
  search?: string | null;
  from?: string | null;
  to?: string | null;
  page: number;
  pageSize: number;
}
