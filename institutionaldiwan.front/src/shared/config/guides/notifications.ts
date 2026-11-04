import type { GuideEntry, PageGuide } from './types'

export const NOTIFICATIONS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'notifications.grid',
    title: 'صندوق الإشعارات',
    body: 'كل صف إشعار وصلك أو وصل إلى قسمك: وثيقة جديدة أو إحالة أُرسلت أو استُلمت أو رُفضت. انقر العنوان لفتح الوثيقة؛ ويُعلَّم الإشعار مقروءًا تلقائيًا.',
    note: 'لا يصلك إشعار عن إجراء نفّذته أنت، والوثائق السرية لا يُعلَن عنها إلا لمنشئها وأصحاب المنح.',
  },
  {
    id: 'notifications.filters',
    title: 'مرشحات الإشعارات',
    body: 'اعرض غير المقروء فقط، أو اختر نوع الإشعار ويظهر بجانبه عدد غير المقروء منه. المرشحات والبحث والترتيب تُحفظ في العنوان فيمكن مشاركة الرابط.',
  },
  {
    id: 'notifications.read-all',
    title: 'قراءة الكل',
    body: 'يعلّم كل إشعاراتك غير المقروءة كمقروءة دفعة واحدة، ويصفّر العدّاد في الجرس. لا يحذف شيئًا.',
    note: 'لإخفاء إشعار نهائيًا من القائمة استخدم «إخفاء الإشعار» من قائمة الصف.',
  },
]

export const NOTIFICATIONS_GUIDE: PageGuide = {
  pageId: 'notifications',
  intro:
    'هنا تتابع كل ما يخصك من تنبيهات النظام. يصل الجديد منها لحظيًا إلى الجرس في أعلى الصفحة، ويمكنك البحث والتصفية والترتيب هنا.',
  entryIds: [
    'notifications.grid',
    'notifications.filters',
    'notifications.read-all',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
