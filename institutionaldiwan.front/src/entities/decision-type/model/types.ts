/** `DecisionTypeResponse` — an administrator-managed kind of decision. */
export interface DecisionTypeResponse {
  decisionTypeId: string
  nameAr: string
  isAppealable: boolean
  effectId: number
  destinationDepartmentId: string | null
  isActive: boolean
  documentTypeIds: number[]
}

export interface DecisionTypeListParams {
  documentTypeId?: number
  isActive?: boolean
}
