import type { GuideEntry, PageGuide } from './types'

export const INSTITUTION_SETTINGS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'institution-settings.identity',
    title: 'بيانات المؤسسة',
    body: 'الاسم الرسمي للمؤسسة بالعربية والشعار والمنطقة الزمنية الافتراضية. الاسم والشعار يظهران في صفحة الدخول والقائمة الجانبية وعنوان التبويب، ومنطقة التوقيت تحدّد كيف تُعرض التواريخ.',
    note: 'رفع الشعار أو إزالته يسري فورًا دون زر «حفظ الإعدادات»؛ أما الاسم فيُحفظ مع بقية الإعدادات.',
  },
  {
    id: 'institution-settings.central',
    title: 'الديوان المركزي',
    body: 'القسم الرئيسي الذي يمثّل الديوان المركزي في الهيكل. يجب أن يكون قسمًا بلا أب ومن نوع الديوان المركزي.',
    note: 'تغييره يتطلب تسجيل سبب النقل.',
  },
  {
    id: 'institution-settings.archive',
    title: 'ديوان الحفظ',
    body: 'القسم الذي تُحفظ عنده الوثائق نهائيًا. أي ديوان يريد حفظ وثيقة يرسلها بحركة من نوع «للحفظ» إلى الديوان الذي أنشأها، وهذا يرسلها إلى ديوان الحفظ، وعند استلامها هناك تُؤرشف.',
    note: 'اتركه فارغًا لتبقى الأرشفة عند الديوان الذي أنشأ الوثيقة.',
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
    body: 'يحفظ كل المجموعات معًا. لا يُحفظ شيء قبل الضغط، ويُعرض تاريخ آخر تحديث بجانب الزر لتتأكد أن التعديل سُجّل.',
  },
]

export const INSTITUTION_SETTINGS_GUIDE: PageGuide = {
  pageId: 'institution-settings',
  intro:
    'إعدادات المؤسسة قيم عامة يبني عليها النظام: هوية المؤسسة، والديوان المركزي، وديوان الحفظ، وسياسة الترقيم، والموظف المسؤول. عدّل ما تحتاج واحفظ مرة واحدة.',
  entryIds: [
    'institution-settings.identity',
    'institution-settings.central',
    'institution-settings.archive',
    'institution-settings.numbering',
    'institution-settings.responsible',
    'institution-settings.save',
  ],
}
