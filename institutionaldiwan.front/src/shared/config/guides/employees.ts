import type { GuideEntry, PageGuide } from './types'

export const EMPLOYEES_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'employees.grid',
    title: 'جدول الموظفين',
    body: 'سجل الموظفين هو بيانات الوظيفة: الاسم والقسم والمسمى وحالة التفعيل. وهو منفصل عن حساب الدخول؛ يمكن ربط موظف بحساب مستخدم لاحقًا حين تحتاج العمليات إلى بيانات وظيفته.',
    note: 'إيقاف تفعيل موظف لا يحذف سجله، فتبقى الوثائق والحركات المنسوبة إليه سليمة.',
  },
  {
    id: 'employees.create',
    title: 'موظف جديد',
    body: 'يفتح نموذج إضافة موظف وربطه بقسمه. اختر القسم بعناية لأنه يحدّد ظهور الموظف في شجرة الأقسام وفي اختيار المرسل والمستلم.',
  },
]

export const EMPLOYEES_GUIDE: PageGuide = {
  pageId: 'employees',
  intro:
    'هنا تُدار سجلات الموظفين وارتباطهم بالأقسام. الموظف ليس حساب دخول بالضرورة، وما تضيفه هنا يظهر في الأقسام وعند اختيار الجهات المسؤولة.',
  entryIds: [
    'employees.grid',
    'employees.create',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
