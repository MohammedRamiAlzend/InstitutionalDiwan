import type { GuideEntry, PageGuide } from './types'

export const SYSTEM_ERRORS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'system-errors.grid',
    title: 'الأخطاء البرمجية',
    body: 'كل صف خطأ واحد مهما تكرر، مع عدد مراته وأول وآخر ظهور. افتح الصف لترى التفاصيل التقنية وتحدد حالته وتكتب ملاحظاتك.',
    note: 'الخطأ المحلول يعود «جديدًا» إذا ظهر مرة أخرى.',
  },
  {
    id: 'system-errors.filters',
    title: 'مرشحات الأخطاء',
    body: 'ضيّق النتائج بالحالة أو المصدر أو النوع أو تاريخ آخر ظهور. المرشحات تُحفظ في العنوان فيمكن مشاركة الرابط.',
  },
  {
    id: 'system-errors.export',
    title: 'التصدير إلى Excel',
    body: 'ينزّل ملف xlsx بكل ما يطابق البحث والمرشحات الحالية، مع التفاصيل التقنية، حتى 10000 صف.',
  },
  {
    id: 'system-errors.purge',
    title: 'حذف الأخطاء القديمة',
    body: 'يحذف نهائيًا الأخطاء التي لم تظهر منذ المدة التي تختارها، مهما كانت حالتها.',
    note: 'لا يمكن التراجع عن الحذف. لا يُحذف شيء تلقائيًا.',
  },
]

export const SYSTEM_ERRORS_GUIDE: PageGuide = {
  pageId: 'system-errors',
  intro: 'سجل المطور لكل خطأ يقع في الخادم أو في متصفحات المستخدمين. تابعها وصدّرها للعمل على إصلاحها.',
  entryIds: [
    'system-errors.grid',
    'system-errors.filters',
    'system-errors.export',
    'system-errors.purge',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
