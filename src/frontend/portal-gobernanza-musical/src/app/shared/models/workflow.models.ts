export interface FichaApprovalStatus {
  fichaId: string;
  status: string;
  reviewedByName: string | null;
  reviewedAtUtc: string | null;
  comment: string | null;
}

export interface ApprovalRecord {
  id: string;
  fichaId: string;
  actorEmail: string;
  actorName: string;
  action: string;
  comment: string | null;
  timestampUtc: string;
}
