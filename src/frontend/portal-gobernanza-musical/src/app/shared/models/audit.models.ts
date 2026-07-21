export interface AuditLog {
  id: string;
  userEmail: string;
  userDisplayName: string;
  ipAddress: string | null;
  entityName: string;
  entityId: string;
  operation: string;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  timestampUtc: string;
}

export interface AuditLogQuery {
  entityName?: string | null;
  entityId?: string | null;
  page: number;
  pageSize: number;
}
