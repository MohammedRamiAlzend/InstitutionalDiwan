import type { GuideEntry, PageGuide } from './types'

export const INSTITUTION_SETTINGS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'institution-settings.identity',
    title: 'بيانات المؤسسة',
    body: 'الاسم الرسمي للمؤسسة بالعربية والشعار والمنطقة الزمنية الافتراضية. هذه البيانات تظهر في الوثائق والمطبوعات، ومنطقة التوقيت تحدّد كيف تُعرض التواريخ.',
  },
  {
    id: 'institution-settings.central',
    title: 'الديوان المركزي',
    body: 'القسم الرئيسي الذي يمثّل الديوان المركزي في الهيكل. يجب أن يكون قسمًا بلا أب ومن نوع الديوان المركزي.',
    note: 'تغييره يتطلب تسجيل سبب النقل.',
  },
  {
    id: 'institution-settings.numbering',
    title: 'الترقيم',
    body: 'النمط الافتراضي لأرقام الوثائق الصادرة وسياسة تصفير التسلسل. تُطبَّق هذه القيم على السجلات الجديدة ما لم تُحدد غيرها.',
  },
  {
    id: 'institution-settings.responsible',
    title: 'الجهة المسؤولة',
    body: 'الموظف المسؤول عن متابعة إعدادات المؤسسة والترقيم. اختره من سجل الموظفين، وتأكد أن سجله مفعّل.',
  },
  {
    id: 'institution-settings.save',
    title: 'حفظ الإعدادات',
    body: 'يحفظ كل المجموعات الأربع معًا. لا يُحفظ شيء قبل الضغط، ويُعرض تاريخ آخر تحديث بجانب الزر لتتأكد أن التعديل سُجّل.',
  },
]

export const INSTITUTION_SETTINGS_GUIDE: PageGuide = {
  pageId: 'institution-settings',
  intro:
    'إعدادات المؤسسة قيم عامة يبني عليها النظام: هوية المؤسسة، والديوان المركزي، وسياسة الترقيم، والموظف المسؤول. عدّل ما تحتاج واحفظ مرة واحدة.',
  entryIds: [
    'institution-settings.identity',
    'institution-settings.central',
    'institution-settings.numbering',
    'institution-settings.responsible',
    'institution-settings.save',
  ],
}
