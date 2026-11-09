import type { GuideEntry, PageGuide } from './types'

export const MOVEMENTS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'movements.tabs',
    title: 'الوارد والصادر',
    body: 'الوارد يعرض الحركات الموجهة إلى قسمك المبدئي، والصادر يعرض ما أرسله قسمك. بدّل بين الصندوقين من هنا؛ ويحفظ العنوان الاختيار فتستطيع مشاركة الرابط.',
  },
  {
    id: 'movements.queue',
    title: 'قائمة الحركات',
    body: 'كل صف حركة وثيقة بين قسمين: اسم الوثيقة ورقمها الرسمي والتعليمات والحالة. افتح الحركة من قائمة الصف لتصل إلى صفحة الوثيقة التي تُدار منها.',
    note: 'الصندوقان خاصان بقسمك المبدئي، فلا ترى حركات الأقسام الأخرى.',
  },
  {
    id: 'movements.filters',
    title: 'مرشحات الحركات',
    body: 'ضيّق القائمة بحالة الحركة أو نوعها، وامسح المرشحات للعودة إلى كل الحركات. قيم المرشحات تُحفظ في عنوان الصفحة.',
  },
]

export const MOVEMENTS_GUIDE: PageGuide = {
  pageId: 'movements',
  intro:
    'الحركات هي مسار الوثائق بين الأقسام. تابع من هنا ما وصل إلى قسمك وما أرسله، ثم افتح الوثيقة لإنجاز الإجراء المطلوب.',
  entryIds: [
    'movements.tabs',
    'movements.queue',
    'movements.filters',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
