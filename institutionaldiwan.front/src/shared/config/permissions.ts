/**
 * Canonical permission codes — must match the API's
 * `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly (16 codes).
 */
export const Permissions = {
  DocumentView: 'DOCUMENT_VIEW',
  DocumentCreate: 'DOCUMENT_CREATE',
  DocumentEdit: 'DOCUMENT_EDIT',
  DocumentDelete: 'DOCUMENT_DELETE',
  DocumentApprove: 'DOCUMENT_APPROVE',
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
} as const

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions]

export const AllPermissions: readonly PermissionCode[] = Object.values(Permissions)
