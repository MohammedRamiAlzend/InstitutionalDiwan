import {
  ASSIGNMENT_TYPE,
  AUDIT_ACTION_TYPE,
  AUDIT_ENTITY_TYPE,
  DEPARTMENT_TYPE,
  DOCUMENT_CONFIDENTIALITY,
  DOCUMENT_DIRECTION,
  DOCUMENT_PRIORITY,
  DOCUMENT_STATUS,
  DOCUMENT_TYPE,
  MOVEMENT_EVENT_TYPE,
  MOVEMENT_STATUS,
  MOVEMENT_STEP_STATUS,
  MOVEMENT_TYPE,
  NUMBERING_MODE,
  NUMBERING_RESET_POLICY,
  ORGANIZATION_TYPE,
  PARTICIPANT_ROLE,
  PARTICIPANT_TARGET_TYPE,
  USER_SCOPE_TYPE,
} from '@/shared/config'
import type { LookupCatalogEntry } from './types'

export {
  ASSIGNMENT_TYPE,
  AUDIT_ACTION_TYPE,
  AUDIT_ENTITY_TYPE,
  DEPARTMENT_TYPE,
  DOCUMENT_CONFIDENTIALITY,
  DOCUMENT_DIRECTION,
  DOCUMENT_PRIORITY,
  DOCUMENT_STATUS,
  DOCUMENT_TYPE,
  DOCUMENT_TYPE_DIRECTION,
  MOVEMENT_EVENT_TYPE,
  MOVEMENT_STATUS,
  MOVEMENT_STEP_STATUS,
  MOVEMENT_TYPE,
  NUMBERING_MODE,
  NUMBERING_RESET_POLICY,
  ORGANIZATION_TYPE,
  PARTICIPANT_ROLE,
  PARTICIPANT_TARGET_TYPE,
  USER_SCOPE_TYPE,
} from '@/shared/config'

/** Catalogs Module 01/02/03 (pickers, settings, and document forms) consume directly. */
export const LOOKUP_SLUGS = {
  DepartmentTypes: 'department-types',
  UserScopeTypes: 'user-scope-types',
  AssignmentTypes: 'assignment-types',
  AccessLevels: 'access-levels',
  NumberingModes: 'numbering-modes',
  NumberingResetPolicies: 'numbering-reset-policies',
  DocumentDirections: 'document-directions',
  DocumentTypes: 'document-types',
  DocumentStatusTypes: 'document-status-types',
  DocumentConfidentialityLevels: 'document-confidentiality-levels',
  DocumentPriorityLevels: 'document-priority-levels',
  ParticipantRoles: 'participant-roles',
  ParticipantTargetTypes: 'participant-target-types',
  OrganizationTypes: 'organization-types',
  MovementTypes: 'movement-types',
  MovementStatuses: 'movement-statuses',
  MovementStepStatuses: 'movement-step-statuses',
  MovementEventTypes: 'movement-event-types',
  AuditActionTypes: 'audit-action-types',
  AuditEntityTypes: 'audit-entity-types',
} as const

export type LookupSlug = (typeof LOOKUP_SLUGS)[keyof typeof LOOKUP_SLUGS]

/**
 * Seeded catalogs mirroring `InstitutionalDiwan.Infrastructure/Persistence/Seed`.
 * They keep Arabic labels usable before the first response, or when the caller
 * lacks the lookup permission.
 */
export const LOOKUP_FALLBACKS: Record<string, readonly LookupCatalogEntry[]> = {
  'user-scope-types': [
    { id: USER_SCOPE_TYPE.Department, desc: 'Own department', descAr: 'الدائرة' },
    { id: USER_SCOPE_TYPE.DepartmentTree, desc: 'Department and sub-departments', descAr: 'الدائرة والفروع' },
    { id: USER_SCOPE_TYPE.Institution, desc: 'Whole institution', descAr: 'المؤسسة' },
    { id: USER_SCOPE_TYPE.System, desc: 'System', descAr: 'النظام' },
  ],
  'assignment-types': [
    { id: ASSIGNMENT_TYPE.Primary, desc: 'Primary', descAr: 'أساسي' },
    { id: ASSIGNMENT_TYPE.Additional, desc: 'Additional', descAr: 'إضافي' },
  ],
  'department-types': [
    { id: DEPARTMENT_TYPE.Directorate, desc: 'Directorate', descAr: 'مديرية' },
    { id: DEPARTMENT_TYPE.Department, desc: 'Department', descAr: 'قسم' },
    { id: DEPARTMENT_TYPE.Section, desc: 'Section', descAr: 'شعبة' },
    { id: DEPARTMENT_TYPE.Unit, desc: 'Unit', descAr: 'وحدة' },
    { id: DEPARTMENT_TYPE.CentralDiwan, desc: 'Central Diwan', descAr: 'الديوان المركزي' },
  ],
  'access-levels': [
    { id: 1, desc: 'View', descAr: 'عرض' },
    { id: 2, desc: 'Download', descAr: 'تنزيل' },
    { id: 3, desc: 'Forward', descAr: 'إحالة' },
    { id: 4, desc: 'Manage', descAr: 'إدارة' },
  ],
  'numbering-modes': [
    { id: NUMBERING_MODE.Automatic, desc: 'Automatic', descAr: 'تلقائي' },
    { id: NUMBERING_MODE.Manual, desc: 'Manual', descAr: 'يدوي' },
  ],
  'numbering-reset-policies': [
    { id: NUMBERING_RESET_POLICY.Yearly, desc: 'Yearly', descAr: 'سنوي' },
    { id: NUMBERING_RESET_POLICY.Monthly, desc: 'Monthly', descAr: 'شهري' },
    { id: NUMBERING_RESET_POLICY.Never, desc: 'Never', descAr: 'لا يتصفّر' },
  ],
  'document-directions': [
    { id: DOCUMENT_DIRECTION.Internal, desc: 'Internal', descAr: 'داخلي' },
    { id: DOCUMENT_DIRECTION.Incoming, desc: 'Incoming', descAr: 'وارد' },
    { id: DOCUMENT_DIRECTION.Outgoing, desc: 'Outgoing', descAr: 'صادر' },
    { id: DOCUMENT_DIRECTION.All, desc: 'All directions', descAr: 'كل الاتجاهات' },
  ],
  'document-types': [
    { id: DOCUMENT_TYPE.IncomingLetter, desc: 'Incoming letter', descAr: 'كتاب وارد' },
    { id: DOCUMENT_TYPE.OutgoingLetter, desc: 'Outgoing letter', descAr: 'كتاب صادر' },
    { id: DOCUMENT_TYPE.InternalMemo, desc: 'Internal memo', descAr: 'مذكرة داخلية' },
    { id: DOCUMENT_TYPE.Circular, desc: 'Circular', descAr: 'تعميم' },
    { id: DOCUMENT_TYPE.Decision, desc: 'Decision', descAr: 'قرار' },
  ],
  'document-status-types': [
    { id: DOCUMENT_STATUS.Draft, desc: 'Draft', descAr: 'مسودة' },
    { id: DOCUMENT_STATUS.Registered, desc: 'Registered', descAr: 'مسجل' },
    { id: DOCUMENT_STATUS.InMovement, desc: 'In movement', descAr: 'قيد التداول' },
    { id: DOCUMENT_STATUS.Archived, desc: 'Archived', descAr: 'مؤرشف' },
    { id: DOCUMENT_STATUS.Canceled, desc: 'Canceled', descAr: 'ملغي' },
  ],
  'document-confidentiality-levels': [
    { id: DOCUMENT_CONFIDENTIALITY.Normal, desc: 'Normal', descAr: 'عادي' },
    { id: DOCUMENT_CONFIDENTIALITY.Confidential, desc: 'Confidential', descAr: 'سري' },
    { id: DOCUMENT_CONFIDENTIALITY.Secret, desc: 'Secret', descAr: 'سري للغاية' },
    { id: DOCUMENT_CONFIDENTIALITY.TopSecret, desc: 'Top secret', descAr: 'بالغ السرية' },
  ],
  'document-priority-levels': [
    { id: DOCUMENT_PRIORITY.Normal, desc: 'Normal', descAr: 'عادي' },
    { id: DOCUMENT_PRIORITY.Urgent, desc: 'Urgent', descAr: 'عاجل' },
    { id: DOCUMENT_PRIORITY.Immediate, desc: 'Immediate', descAr: 'فوري' },
  ],
  'participant-roles': [
    { id: PARTICIPANT_ROLE.Sender, desc: 'Sender', descAr: 'مُرسل' },
    { id: PARTICIPANT_ROLE.Receiver, desc: 'Receiver', descAr: 'مستلم' },
    { id: PARTICIPANT_ROLE.Cc, desc: 'CC', descAr: 'نسخة' },
    { id: PARTICIPANT_ROLE.Signatory, desc: 'Signatory', descAr: 'موقّع' },
  ],
  'participant-target-types': [
    { id: PARTICIPANT_TARGET_TYPE.ExternalOrganization, desc: 'External organization', descAr: 'جهة خارجية' },
    { id: PARTICIPANT_TARGET_TYPE.InternalDepartment, desc: 'Internal department', descAr: 'قسم داخلي' },
    { id: PARTICIPANT_TARGET_TYPE.Person, desc: 'Person', descAr: 'شخص' },
  ],
  'organization-types': [
    { id: ORGANIZATION_TYPE.Government, desc: 'Government', descAr: 'حكومي' },
    { id: ORGANIZATION_TYPE.Private, desc: 'Private', descAr: 'خاص' },
    { id: ORGANIZATION_TYPE.International, desc: 'International', descAr: 'دولي' },
    { id: ORGANIZATION_TYPE.Individual, desc: 'Individual', descAr: 'فرد' },
  ],
  'movement-types': [
    { id: MOVEMENT_TYPE.Referral, desc: 'Referral', descAr: 'إحالة' },
    { id: MOVEMENT_TYPE.Transfer, desc: 'Transfer', descAr: 'تحويل' },
    { id: MOVEMENT_TYPE.Cc, desc: 'CC', descAr: 'نسخة' },
    { id: MOVEMENT_TYPE.Return, desc: 'Return', descAr: 'إعادة' },
  ],
  'movement-statuses': [
    { id: MOVEMENT_STATUS.Draft, desc: 'Draft', descAr: 'مسودة' },
    { id: MOVEMENT_STATUS.Sent, desc: 'Sent', descAr: 'مُرسل' },
    { id: MOVEMENT_STATUS.Received, desc: 'Received', descAr: 'مستلم' },
    { id: MOVEMENT_STATUS.InProgress, desc: 'In progress', descAr: 'قيد المعالجة' },
    { id: MOVEMENT_STATUS.Completed, desc: 'Completed', descAr: 'مكتمل' },
    { id: MOVEMENT_STATUS.Rejected, desc: 'Rejected', descAr: 'مرفوض' },
    { id: MOVEMENT_STATUS.Returned, desc: 'Returned', descAr: 'مُعاد' },
    { id: MOVEMENT_STATUS.Redirected, desc: 'Redirected', descAr: 'مُحوّل' },
    { id: MOVEMENT_STATUS.Closed, desc: 'Closed', descAr: 'مغلق' },
    { id: MOVEMENT_STATUS.Canceled, desc: 'Canceled', descAr: 'ملغي' },
  ],
  'movement-step-statuses': [
    { id: MOVEMENT_STEP_STATUS.Pending, desc: 'Pending', descAr: 'قيد الانتظار' },
    { id: MOVEMENT_STEP_STATUS.Sent, desc: 'Sent', descAr: 'مُرسل' },
    { id: MOVEMENT_STEP_STATUS.Received, desc: 'Received', descAr: 'مستلم' },
    { id: MOVEMENT_STEP_STATUS.Completed, desc: 'Completed', descAr: 'مكتمل' },
    { id: MOVEMENT_STEP_STATUS.Rejected, desc: 'Rejected', descAr: 'مرفوض' },
    { id: MOVEMENT_STEP_STATUS.Returned, desc: 'Returned', descAr: 'مُعاد' },
    { id: MOVEMENT_STEP_STATUS.Redirected, desc: 'Redirected', descAr: 'مُحوّل' },
  ],
  'movement-event-types': [
    { id: MOVEMENT_EVENT_TYPE.Created, desc: 'Created', descAr: 'إنشاء' },
    { id: MOVEMENT_EVENT_TYPE.Sent, desc: 'Sent', descAr: 'إرسال' },
    { id: MOVEMENT_EVENT_TYPE.Received, desc: 'Received', descAr: 'استلام' },
    { id: MOVEMENT_EVENT_TYPE.Completed, desc: 'Completed', descAr: 'إكمال' },
    { id: MOVEMENT_EVENT_TYPE.Rejected, desc: 'Rejected', descAr: 'رفض' },
    { id: MOVEMENT_EVENT_TYPE.Returned, desc: 'Returned', descAr: 'إعادة' },
    { id: MOVEMENT_EVENT_TYPE.Redirected, desc: 'Redirected', descAr: 'تحويل' },
    { id: MOVEMENT_EVENT_TYPE.Closed, desc: 'Closed', descAr: 'إغلاق' },
    { id: MOVEMENT_EVENT_TYPE.Canceled, desc: 'Canceled', descAr: 'إلغاء' },
  ],
  'audit-action-types': [
    { id: AUDIT_ACTION_TYPE.Create, desc: 'Create', descAr: 'إنشاء' },
    { id: AUDIT_ACTION_TYPE.Update, desc: 'Update', descAr: 'تعديل' },
    { id: AUDIT_ACTION_TYPE.Delete, desc: 'Delete', descAr: 'حذف' },
    { id: AUDIT_ACTION_TYPE.View, desc: 'View', descAr: 'عرض' },
    { id: AUDIT_ACTION_TYPE.Login, desc: 'Login', descAr: 'تسجيل دخول' },
    { id: AUDIT_ACTION_TYPE.Logout, desc: 'Logout', descAr: 'تسجيل خروج' },
    { id: AUDIT_ACTION_TYPE.Approve, desc: 'Approve', descAr: 'اعتماد' },
    { id: AUDIT_ACTION_TYPE.Execute, desc: 'Execute', descAr: 'تنفيذ' },
  ],
  'audit-entity-types': [
    { id: AUDIT_ENTITY_TYPE.Document, desc: 'Document', descAr: 'وثيقة' },
    { id: AUDIT_ENTITY_TYPE.Department, desc: 'Department', descAr: 'دائرة' },
    { id: AUDIT_ENTITY_TYPE.User, desc: 'User', descAr: 'مستخدم' },
    { id: AUDIT_ENTITY_TYPE.Role, desc: 'Role', descAr: 'دور' },
    { id: AUDIT_ENTITY_TYPE.Movement, desc: 'Movement', descAr: 'إحالة' },
    { id: AUDIT_ENTITY_TYPE.File, desc: 'File', descAr: 'ملف' },
    { id: AUDIT_ENTITY_TYPE.NumberingRegistry, desc: 'Numbering registry', descAr: 'سجل ترقيم' },
    { id: AUDIT_ENTITY_TYPE.Setting, desc: 'Setting', descAr: 'إعداد' },
    { id: AUDIT_ENTITY_TYPE.AuditLog, desc: 'Audit log', descAr: 'سجل التدقيق' },
    { id: AUDIT_ENTITY_TYPE.DocumentNumber, desc: 'Official number', descAr: 'رقم رسمي' },
    { id: AUDIT_ENTITY_TYPE.DocumentRelation, desc: 'Document relation', descAr: 'ربط وثيقة' },
    {
      id: AUDIT_ENTITY_TYPE.ExternalOrganization,
      desc: 'External organization',
      descAr: 'جهة خارجية',
    },
    { id: AUDIT_ENTITY_TYPE.Operation, desc: 'Operation', descAr: 'عملية' },
  ],
}

/** Looks up a seeded label when the catalog response has not arrived. */
export function findLookupFallback(
  lookup: string,
  id: number,
): LookupCatalogEntry | undefined {
  return LOOKUP_FALLBACKS[lookup]?.find((entry) => entry.id === id)
}
