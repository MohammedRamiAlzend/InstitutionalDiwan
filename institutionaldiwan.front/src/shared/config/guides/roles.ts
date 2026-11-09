import type { GuideEntry, PageGuide } from './types'

export const ROLES_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'roles.grid',
    title: 'جدول الأدوار',
    body: 'الدور مجموعة صلاحيات تُسنَد إلى المستخدمين دفعة واحدة بدل منحها لكل حساب على حدة. يعرض الجدول اسم كل دور ورمزه وعدد صلاحياته، ومن قائمة الصف تعدّل بياناته أو تضبط صلاحياته من مصفوفة الصلاحيات.',
    note: 'تغيير صلاحيات دور يسري على كل من أُسند إليه هذا الدور عند دخوله التالي.',
  },
  {
    id: 'roles.create',
    title: 'دور جديد',
    body: 'يفتح نموذج إنشاء دور بالاسم العربي والرمز ووصف مختصر. بعد الإنشاء افتح قائمة صف الدور واختر صلاحياته؛ الدور الجديد بلا صلاحيات حتى تحدّدها.',
  },
]

export const ROLES_GUIDE: PageGuide = {
  pageId: 'roles',
  intro:
    'الأدوار تحدّد ما يستطيع المستخدم فعله في النظام. أنشئ الدور مرة، وحدّد صلاحياته من المصفوفة، ثم أسنده لمن يحتاجه من صفحة المستخدمين.',
  entryIds: [
    'roles.grid',
    'roles.create',
    'table.search',
    'table.columns',
    'table.export',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
