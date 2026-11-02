import type { GuideEntry, PageGuide } from './types'

export const DASHBOARD_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'dashboard.welcome',
    title: 'بطاقة الترحيب',
    body: 'تؤكد أنك مسجّل الدخول بأي حساب، وهذا الحساب هو ما تُنسب إليه إجراءاتك في السجلات.',
  },
  {
    id: 'dashboard.stats',
    title: 'مؤشرات حسابك',
    body: 'عدد الصلاحيات الفعّالة لحسابك وأدواره. تفسّر لك سبب ظهور روابط في القائمة الجانبية دون غيرها: ما لا تملك صلاحيته لا يظهر.',
  },
  {
    id: 'dashboard.next',
    title: 'ما التالي؟',
    body: 'خارطة الوحدات القادمة. العناصر غير الجاهزة تظهر في القائمة الجانبية بوسم «قريبًا» بدل فتح صفحات فارغة.',
  },
]

export const DASHBOARD_GUIDE: PageGuide = {
  pageId: 'dashboard',
  intro:
    'لوحة التحكم نظرة سريعة على حسابك وما هو متاح لك في مساحة العمل. منها تبدأ، ثم تنتقل إلى الصفحات من القائمة الجانبية.',
  entryIds: ['dashboard.welcome', 'dashboard.stats', 'dashboard.next'],
}
