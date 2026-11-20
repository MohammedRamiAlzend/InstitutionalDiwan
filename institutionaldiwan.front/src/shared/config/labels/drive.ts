/** Module 11 Drive (الملفات): foundational catalog seeded in backend Phase 5 ahead of the separate
 * front-end plan (`plans/module-11-drive-frontend.md`). The contract labels mirror `DriveCatalog.cs`
 * (space types, item kinds, roles, principal types, version status/source, deletion-request status,
 * edit-session status); the rest are the core domain nouns/actions/quota terms the module already
 * exposes. Screen-specific copy (grids, dialogs, toasts, Zod) and the `error.drive.*` codes land with
 * the front-end plan, so these keys are expected orphan warnings until then. Data-driven text (space/
 * folder/file names, user/department names, sizes, digests) stays out. */
export const DRIVE_LABELS = {
  'drive.title': 'الملفات',
  'drive.my-files': 'ملفاتي',
  'drive.shared-spaces': 'المساحات المشتركة',
  'drive.starred': 'المفضلة',
  'drive.trash': 'سلة المحذوفات',

  'drive.space-type.personal': 'مساحة شخصية',
  'drive.space-type.shared': 'مساحة مشتركة',

  'drive.item-kind.folder': 'مجلد',
  'drive.item-kind.file': 'ملف',

  'drive.role.viewer': 'مشاهد',
  'drive.role.editor': 'محرر',
  'drive.role.manager': 'مدير',

  'drive.principal.user': 'مستخدم',
  'drive.principal.department': 'قسم',

  'drive.version-status.pending': 'قيد الرفع',
  'drive.version-status.committed': 'معتمد',
  'drive.version-status.abandoned': 'ملغى',

  'drive.version-source.upload': 'رفع',
  'drive.version-source.editor-save': 'حفظ المحرر',
  'drive.version-source.restore': 'استرجاع إصدار',

  'drive.deletion-status.pending': 'قيد المراجعة',
  'drive.deletion-status.approved': 'موافَق عليه',
  'drive.deletion-status.rejected': 'مرفوض',
  'drive.deletion-status.cancelled': 'ملغى',

  'drive.session-status.open': 'مفتوحة',
  'drive.session-status.saved': 'محفوظة',
  'drive.session-status.closed': 'مغلقة',
  'drive.session-status.expired': 'منتهية',

  'drive.action.upload': 'رفع ملف',
  'drive.action.download': 'تنزيل',
  'drive.action.new-folder': 'مجلد جديد',
  'drive.action.new-space': 'مساحة جديدة',
  'drive.action.rename': 'إعادة تسمية',
  'drive.action.move': 'نقل',
  'drive.action.star': 'تمييز بنجمة',
  'drive.action.unstar': 'إزالة النجمة',
  'drive.action.edit': 'تحرير',
  'drive.action.versions': 'الإصدارات',
  'drive.action.restore-version': 'استرجاع هذا الإصدار',
  'drive.action.permissions': 'الصلاحيات',
  'drive.action.request-deletion': 'طلب حذف',
  'drive.action.restore': 'استعادة',
  'drive.action.permanent-delete': 'حذف نهائي',

  'drive.permission.inherited': 'موروثة',
  'drive.permission.grant': 'منح صلاحية',
  'drive.permission.revoke': 'سحب الصلاحية',
  'drive.permission.break-inheritance': 'كسر الوراثة',
  'drive.permission.restore-inheritance': 'استعادة الوراثة',

  'drive.version.current': 'الحالي',
  'drive.version.number': 'الإصدار {number}',

  'drive.quota': 'الحصة التخزينية',
  'drive.quota.used': 'المستخدم',
  'drive.quota.available': 'المتاح',
  'drive.quota.usage': '{used} من {quota}',

  'drive.trash.retention': 'تُحذف العناصر نهائيًا بعد {days} يومًا في السلة.',
  'drive.deletion.approver-hint': 'يتطلب الحذف موافقة رئيس القسم، أو المعتمِد ADMIN عند غياب رئيس.',
} as const