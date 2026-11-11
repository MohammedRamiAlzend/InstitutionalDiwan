import type { GuideEntry, PageGuide } from './types'

export const DECISIONS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'decisions.tabs',
    title: 'سجل القرارات وأنواعها',
    body: 'السجل يعرض القرارات على الوثائق التي تملك الاطلاع عليها. تبويب الأنواع لمدير القوائم: يضيف نوعًا ويسمّي أثره، ويوقف النوع الذي استُخدم بدل حذفه.',
    note: 'تبويب الأنواع يظهر لمن يملك إدارة القوائم.',
  },
  {
    id: 'decisions.register',
    title: 'سجل القرارات',
    body: 'كل صف قرار على وثيقة تستطيع رؤيتها. البحث يشمل رقم القرار واسم الوثيقة ورقمها، والمرشحات تُحفظ في العنوان.',
    note: 'إلغاء إدخال قرار أُدخل خطأ لمدير النظام فقط، ومن قائمة الصف.',
  },
  {
    id: 'decisions.types',
    title: 'أنواع القرارات',
    body: 'نوع الإحالة بلا قسم وجهة يُحفظ موقوفًا حتى يُسمّى القسم ويُفعَّل. تعديل النوع لا يغيّر القرارات التي صدرت به.',
  },
  {
    id: 'decisions.type-name',
    title: 'اسم نوع القرار',
    body: 'هذا الاسم هو ما يراه المستخدم عند إصدار القرار.',
  },
  {
    id: 'decisions.type-effect',
    title: 'أثر نوع القرار',
    body: 'الإحالة تقترح توجيه الوثيقة إلى قسم الوجهة. الإرسال للحفظ يقترح طريق الأرشفة. التوحيد يربط الملف بملف رئيسي دون تغيير رقمه أو مكانه. «بلا أثر» يسجّل القرار دون اقتراح خطوة.',
    note: 'قسم الوجهة يظهر لنوع الإحالة فقط.',
  },
  {
    id: 'decisions.cancel-reason',
    title: 'سبب إلغاء الإدخال',
    body: 'يُطلب السبب لأن الإلغاء يُبقي الرقم محجوزًا لدى الديوان في تلك السنة، ويُسجَّل في أثر التدقيق.',
  },
]

export const DECISIONS_GUIDE: PageGuide = {
  pageId: 'decisions',
  intro:
    'القرارات تُصدر من الديوان الذي يحمل الوثيقة. هذه الصفحة تعرض ما صدر، وتتيح لمدير القوائم تجهيز الأنواع.',
  entryIds: [
    'decisions.tabs',
    'decisions.register',
    'decisions.types',
    'decisions.type-name',
    'decisions.type-effect',
    'decisions.cancel-reason',
    'table.search',
    'table.columns',
    'table.export',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
