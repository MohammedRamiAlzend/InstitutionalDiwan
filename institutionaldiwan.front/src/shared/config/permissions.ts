/**
 * Canonical permission codes — must match the API's
 * `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly (22 codes).
 */
export const Permissions = {
  DocumentView: 'DOCUMENT_VIEW',
  DocumentCreate: 'DOCUMENT_CREATE',
  DocumentEdit: 'DOCUMENT_EDIT',
  DocumentDelete: 'DOCUMENT_DELETE',
  DocumentApprove: 'DOCUMENT_APPROVE',
  DocumentArchive: 'DOCUMENT_ARCHIVE',
  NumberingView: 'NUMBERING_VIEW',
  NumberingManage: 'NUMBERING_MANAGE',
  DepartmentManage: 'DEPARTMENT_MANAGE',
  RoleManage: 'ROLE_MANAGE',
  UserManage: 'USER_MANAGE',
  EmployeeManage: 'EMPLOYEE_MANAGE',
  AccessGrantManage: 'ACCESS_GRANT_MANAGE',
  ReportView: 'REPORT_VIEW',
  LookupManage: 'LOOKUP_MANAGE',
  AuditView: 'AUDIT_VIEW',
  AuditManage: 'AUDIT_MANAGE',
  SystemErrorsView: 'SYSTEM_ERRORS_VIEW',
  PersonView: 'PERSON_VIEW',
  PersonManage: 'PERSON_MANAGE',
  /** Read the records other departments keep on their visits of a document. */
  DepartmentRecordViewAll: 'DEPARTMENT_RECORD_VIEW_ALL',
  /** Correct a department record after it was locked, with a reason. */
  DepartmentRecordCorrect: 'DEPARTMENT_RECORD_CORRECT',
} as const

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions]

export const AllPermissions: readonly PermissionCode[] = Object.values(Permissions)
