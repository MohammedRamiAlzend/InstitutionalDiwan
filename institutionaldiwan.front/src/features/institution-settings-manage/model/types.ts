/** `PUT /api/institution-settings` payload; empty optional text is sent as `null`. */
export interface UpdateInstitutionSettingsRequest {
  institutionNameAr: string
  numberingModeId: number
  numberingResetPolicyId: number
  centralDiwanDepartmentId: string
  institutionNameEn: string | null
  logoPath: string | null
  defaultTimezone: string | null
  responsibleEmployeeId: string | null
  /** Required only when the Central Diwan department changes. */
  centralDiwanTransferReason: string | null
}
