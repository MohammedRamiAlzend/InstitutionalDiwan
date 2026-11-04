import type { GuideEntry, PageGuide } from './types'

export const LOOKUPS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'lookups.catalog',
    title: 'اختيار الجدول المرجعي',
    body: 'الجداول مجمّعة حسب الوحدة. اختر جدولًا لعرض قيمه؛ يُحفظ اختيارك في العنوان فيمكن مشاركة الرابط.',
  },
  {
    id: 'lookups.grid',
    title: 'قيم الجدول',
    body: 'كل صف قيمة مرجعية بمعرّف ثابت وتسمية عربية وإنجليزية. القيم الموقوفة تبقى ظاهرة هنا لكنها تختفي من القوائم المنسدلة.',
    note: 'المعرّف لا يتغير بعد الإنشاء.',
  },
  {
    id: 'lookups.add',
    title: 'إضافة قيمة',
    body: 'اختر معرّفًا موجبًا غير مستخدم في هذا الجدول (يُقترح التالي تلقائيًا) وأدخل التسميتين. تظهر القيمة فورًا في القوائم المنسدلة.',
    note: 'بعض الجداول (مزوّدو التخزين وأنواع الإشعارات) لا تقبل إضافة قيم من هنا.',
  },
  {
    id: 'lookups.status',
    title: 'إيقاف وتفعيل',
    body: 'الإيقاف حذف ناعم يمنع اختيار القيمة مستقبلًا. يُرفض إذا كانت سجلات أخرى تشير إليها، ويمكن إعادة التفعيل في أي وقت.',
    note: 'احذر من إيقاف القيم النظامية؛ يعتمد عليها سلوك النظام.',
  },
]

export const LOOKUPS_GUIDE: PageGuide = {
  pageId: 'lookups',
  intro:
    'هنا تدير القيم المرجعية التي تظهر في القوائم المنسدلة عبر النظام: أنواع الوثائق والأولويات ومواقع الأرشفة وغيرها.',
  entryIds: [
    'lookups.catalog',
    'lookups.grid',
    'lookups.add',
    'lookups.status',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
