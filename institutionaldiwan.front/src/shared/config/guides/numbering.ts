import type { GuideEntry, PageGuide } from './types'

export const NUMBERING_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'numbering.tabs',
    title: 'أقسام صفحة الترقيم',
    body: 'سجلات الترقيم هي التي تصدر منها الأرقام الرسمية بقالبها وعداداتها، وسجل الدوائر يعرض الأرقام التي كتبتها الدوائر في دفاترها.',
  },
  {
    id: 'numbering.registries',
    title: 'سجلات الترقيم',
    body: 'كل سجل نطاق ترقيم له رمز وقالب ومسلسل يعدّ الأرقام الصادرة، وله سياسة تصفير تعيد العدّ بحسب السنة أو غيرها. افتح السجل لرؤية عداداته وتعديل إعداده.',
    note: 'الأرقام الصادرة محفوظة دائمًا، وتعديل القالب لا يغيّر ما صدر سابقًا.',
  },
  {
    id: 'numbering.filters',
    title: 'مرشحات السجلات',
    body: 'ضيّق القائمة بحالة السجل أو سنة الترقيم لتصل إلى السجل المطلوب. المرشحات تُحفظ في العنوان.',
  },
  {
    id: 'numbering.create',
    title: 'سجل جديد',
    body: 'ينشئ سجل ترقيم بنطاقه وقالب أرقامه وسياسة تصفيره. اختر القالب بعناية فهو ما سيظهر في كل رقم رسمي يصدر من هذا السجل.',
    note: 'يتطلب صلاحية إدارة الترقيم.',
  },
  {
    id: 'numbering.register-book',
    title: 'سجل الدوائر',
    body: 'الأرقام التي سجلتها كل دائرة في دفترها الخاص، لتطابقها مع الأرقام الرسمية للمؤسسة وتكشف التضارب مبكرًا.',
  },
]

export const NUMBERING_GUIDE: PageGuide = {
  pageId: 'numbering',
  intro:
    'الترقيم يضبط كيف تُصدر الأرقام الرسمية للوثائق. عرّف سجلات الترقيم وقوالبها، وتابع العدادات، وراجع أرقام الدوائر.',
  entryIds: [
    'numbering.tabs',
    'numbering.registries',
    'numbering.filters',
    'numbering.create',
    'numbering.register-book',
    'table.search',
    'table.columns',
    'table.export',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
