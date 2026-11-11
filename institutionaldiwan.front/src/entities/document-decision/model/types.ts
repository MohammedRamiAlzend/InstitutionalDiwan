/** Reserved until appeals exist. The list on a decision is empty. */
export interface DecisionAppealResponse {
  appealId: string
  appealDate: string
  appellantDepartmentId: string
  referenceNumber: string | null
  notes: string | null
  outcomeId: number
  outcomeDate: string | null
}

/** `DocumentDecisionResponse` — one decision, including the snapshot taken at issuance. */
export interface DocumentDecisionResponse {
  decisionId: string
  documentId: string
  decisionTypeId: string
  typeNameAr: string
  decisionNumber: string
  decisionDate: string
  decisionYear: number
  issuingDepartmentId: string
  registryVisitId: string
  summary: string | null
  statusId: number
  isAppealable: boolean
  effectId: number
  destinationDepartmentId: string | null
  documentFileId: string | null
  recordedByUserId: string
  recordedAt: string
  statusReason: string | null
  appeals: DecisionAppealResponse[]
}

/** One row of `GET /api/decisions`. */
export interface DecisionRegisterItemResponse {
  decisionId: string
  documentId: string
  documentTitle: string
  officialNumberText: string | null
  decisionTypeId: string
  typeNameAr: string
  decisionNumber: string
  decisionDate: string
  decisionYear: number
  issuingDepartmentId: string
  statusId: number
  effectId: number
  isAppealable: boolean
  destinationDepartmentId: string | null
}

export interface DecisionRegisterParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDescending?: boolean
  decisionTypeId?: string
  statusId?: number
  issuingDepartmentId?: string
  year?: number
}

/** `NextActionResponse`. `kind` is null when the screen should offer nothing. */
export interface NextActionResponse {
  kind: 'refer' | 'send-for-archive' | 'archive' | null
  decisionId: string | null
  destinationDepartmentId: string | null
  movementTypeId: number | null
}
