/**
 * Sidebar navigation labels: one key per group and per item, plus the shell chrome
 * around the nav (tagline, menu aria-labels). Keys mirror `shell-nav.ts` ids so the
 * migration is a one-to-one swap of `label` → `labelKey`.
 */
export const NAV_LABELS = {
  'nav.group.overview': 'نظرة عامة',
  /** Standalone section (no visible header): names the Drive list for assistive tech. */
  'nav.group.files': 'الملفات',
  'nav.group.correspondence': 'المراسلات',
  'nav.group.organization': 'التنظيم',
  'nav.group.access': 'إدارة الوصول',
  'nav.group.governance': 'الرقابة والتدقيق',

  'nav.dashboard': 'لوحة التحكم',
  'nav.notifications': 'الإشعارات',
  'nav.documents': 'الوثائق',
  'nav.movements': 'الحركات',
  'nav.decisions': 'القرارات',
  'nav.my-registers': 'سجلاتي',
  'nav.persons': 'الأشخاص',
  'nav.archive': 'الأرشيف',
  'nav.drive': 'الملفات',
  'nav.departments': 'الأقسام',
  'nav.institution-settings': 'إعدادات المؤسسة',
  'nav.users': 'المستخدمون',
  'nav.roles': 'الأدوار والصلاحيات',
  'nav.employees': 'الموظفون',
  'nav.numbering': 'الترقيم',
  'nav.document-schemas': 'مخططات الحقول',
  'nav.lookups': 'الجداول المرجعية',
  'nav.audit': 'سجل التدقيق',
  'nav.system-errors': 'الأخطاء البرمجية',

  'nav.tagline': 'منصة المراسلات الرسمية',
  'nav.main-menu': 'القائمة الرئيسية',
  'nav.expand-menu': 'توسيع القائمة',
  'nav.collapse-menu': 'طي القائمة',
} as const