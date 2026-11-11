import type { GuideEntry, PageGuide } from './types'

export const MY_REGISTERS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'my-registers.filters',
    title: 'اختيار السجل وتصفيته',
    body: 'اختر أحد سجلات قسمك، وبين قوسين عدد أسطره. بعدها صفِّ بالسنة أو حالة الرقم أو نطاق تاريخ التسجيل. كل اختيار يُحفظ في عنوان الصفحة فتستطيع إرسال الرابط كما هو.',
    note: 'إن كان لقسمك سجل واحد فيُفتح مباشرة. عند اختيار نموذج وإصدار تظهر حقوله أعمدةً ويقتصر الجدول على الأسطر المسجّلة بذلك الإصدار؛ والافتراضي هو الأحدث. «تصفية بالحقول» تصفّي بقيم حقول الإصدار المختار. كل البحث والتصفية والترتيب والتصفح يجري على الخادم.',
  },
]

export const MY_REGISTERS_GUIDE: PageGuide = {
  pageId: 'my-registers',
  intro:
    'هذه الصفحة تعرض سجلات قسمك فقط، وكل سجل على حدة: الرقم الذي كتبه قسمك، وتاريخه، والوثيقة، ورقم الديوان المقابل.',
  entryIds: [
    'my-registers.filters',
    'table.search',
    'table.columns',
    'table.export',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
