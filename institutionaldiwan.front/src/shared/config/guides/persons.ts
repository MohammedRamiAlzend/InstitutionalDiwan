import type { GuideEntry, PageGuide } from './types'

export const PERSONS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'persons.grid',
    title: 'الأشخاص',
    body: 'كل شخص ورد اسمه في وثيقة يمكنك الاطلاع عليها، مع عدد وثائقه. اكتب جزءًا من الاسم للبحث؛ لا فرق بين همزات الألف أو التاء المربوطة والهاء.',
    note: 'العدد يشمل الوثائق المسموحة لك فقط.',
  },
  {
    id: 'person-details.documents',
    title: 'وثائق الشخص',
    body: 'كل وثيقة ورد فيها هذا الشخص: صفته فيها، وحالتها، والقسم الموجودة فيه الآن، وآخر إجراء تم عليها.',
  },
  {
    id: 'person-details.timeline',
    title: 'السجل الزمني',
    body: 'كل ما جرى على وثائق هذا الشخص في سجل واحد، الأحدث أولًا: تغيّر الحالات والحركات والأرقام والأرشفة، وكل حدث يذكر وثيقته.',
  },
]

export const PERSONS_GUIDE: PageGuide = {
  pageId: 'persons',
  intro: 'سجل الأشخاص الواردة أسماؤهم في الوثائق من غير مستخدمي النظام، كالمدعى عليهم والمدعين.',
  entryIds: ['persons.grid', 'table.search', 'table.columns', 'table.pageSize', 'table.refresh', 'table.rowMenu', 'table.empty'],
}

export const PERSON_DETAILS_GUIDE: PageGuide = {
  pageId: 'person-details',
  intro: 'ملف شخص واحد: وثائقه وما جرى عليها.',
  entryIds: ['person-details.documents', 'person-details.timeline', 'table.search', 'table.columns', 'table.pageSize', 'table.refresh', 'table.rowMenu', 'table.empty'],
}
