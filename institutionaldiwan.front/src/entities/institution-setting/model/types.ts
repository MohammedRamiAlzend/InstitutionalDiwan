/** `InstitutionSettingResponse` from `/api/institution-settings` (camelCase JSON). */
export interface InstitutionSettingResponse {
  settingId: string
  institutionNameAr: string
  institutionNameEn: string | null
  logoPath: string | null
  numberingModeId: number
  numberingResetPolicyId: number
  defaultTimezone: string
  centralDiwanDepartmentId: string | null
  /** The department that keeps archived files; null while archiving stays with the owning department. */
  archiveDiwanDepartmentId: string | null
  responsibleEmployeeId: string | null
  updatedAt: string
}
