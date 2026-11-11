export interface DecisionTypeWriteRequest {
  nameAr: string
  isAppealable: boolean
  effectId: number
  destinationDepartmentId: string | null
  documentTypeIds: number[]
}
