import type { GuideEntry, PageGuide } from './types'

export const ARCHIVE_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'archive.grid',
    title: 'الوثائق المؤرشفة',
    body: 'كل صف وثيقة مؤرشفة تملك صلاحية الاطلاع عليها، مع موقع أرشفتها وتاريخها. الوثيقة المؤرشفة مجمّدة أمام إضافة المشاركين والمرفقات والأرقام حتى تُلغى أرشفتها.',
    note: 'الأرشفة وإلغاؤها من صفحة تفاصيل الوثيقة.',
  },
  {
    id: 'archive.filters',
    title: 'مرشحات الأرشيف',
    body: 'ضيّق النتائج بموقع الأرشفة أو الاتجاه أو النوع أو القسم المالك أو مدى تاريخ الأرشفة. المرشحات تُحفظ في العنوان فيمكن مشاركة الرابط.',
  },
  {
    id: 'archive.save-search',
    title: 'حفظ البحث',
    body: 'يحفظ البحث والمرشحات الحالية باسم تختاره لتعيدها بنقرة واحدة. عمليات البحث المحفوظة خاصة بك وحدك، وأقصاها 50.',
    note: 'يمكن جعل بحث واحد افتراضيًا ليُطبَّق عند فتح الصفحة دون مرشحات.',
  },
  {
    id: 'archive.saved-searches',
    title: 'عمليات بحثي المحفوظة',
    body: 'انقر اسم البحث لتطبيقه على الجدول. القلم يعيد تسميته أو يستبدل مرشحاته بالحالية، وسلة المحذوفات تحذفه نهائيًا.',
  },
  {
    id: 'archive.location',
    title: 'موقع الأرشفة',
    body: 'المكان الذي تُحفظ فيه الوثيقة: الديوان المركزي أو الأرشيف الورقي أو الإلكتروني. يُسجَّل في سجل الأرشفة ويمكن البحث به لاحقًا.',
  },
]

export const ARCHIVE_GUIDE: PageGuide = {
  pageId: 'archive',
  intro:
    'الأرشيف يجمع الوثائق التي انتهت معالجتها وأُرشفت. ابحث فيها بالمرشحات، واحفظ عمليات البحث المتكررة لتعود إليها بسرعة.',
  entryIds: [
    'archive.grid',
    'archive.filters',
    'archive.save-search',
    'archive.saved-searches',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
