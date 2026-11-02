import type { GuideEntry, PageGuide } from './types'

export const DEPARTMENTS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'departments.create',
    title: 'قسم جديد',
    body: 'ينشئ قسمًا رئيسيًا في الهيكل التنظيمي. لإضافة قسم فرعي افتح قائمة القسم الأب من الشجرة واختر إضافة قسم تابع.',
  },
  {
    id: 'departments.views',
    title: 'طريقة العرض',
    body: 'الأقسام نفسها تُعرض بأربع طرق: شجرة للتنقل والتعديل، ومخطط لرؤية الهيكل دفعة واحدة، وإحصاء لتوزيع الأقسام حسب النوع، وجدول للبحث والفرز. البيانات واحدة والتغيير في الشكل فقط.',
  },
  {
    id: 'departments.search',
    title: 'البحث في الأقسام',
    body: 'ابحث باسم القسم أو رمزه لتظهر الأقسام المطابقة مع آبائها. أثناء البحث يتعطل توسيع الشجرة وطيها.',
  },
  {
    id: 'departments.employees',
    title: 'إظهار فروع الموظفين',
    body: 'يضيف موظفي كل قسم تحت عقدته في الشجرة، فتتبيّن من يعمل أين دون مغادرة الصفحة.',
    note: 'يظهر لمن يملك صلاحية إدارة الموظفين فقط.',
  },
  {
    id: 'departments.expand',
    title: 'توسيع الكل وطيّ الكل',
    body: 'يفتح كل فروع الشجرة أو يغلقها دفعة واحدة. مفيد للمرور على الهيكل كله أو للعودة إلى المستوى الأعلى بسرعة.',
  },
  {
    id: 'departments.content',
    title: 'الهيكل التنظيمي',
    body: 'ما تراه هنا هو الهيكل بالطريقة المختارة. انقر بالزر الأيمن على قسم لتعدّله أو تضيف تابعًا له أو تنقله أو تنسخ معرّفه، وافتح بطاقته لرؤية تفاصيله.',
    note: 'نقل قسم يغيّر موقعه وموقع كل تابعيه في الهيكل.',
  },
]

export const DEPARTMENTS_GUIDE: PageGuide = {
  pageId: 'departments',
  intro:
    'الأقسام تشكّل الهيكل التنظيمي الذي تُبنى عليه الصلاحيات ونطاقات الوصول والحركات. أدر الهيكل من هنا بالطريقة التي تناسبك: شجرة أو مخطط أو إحصاء أو جدول.',
  entryIds: [
    'departments.create',
    'departments.views',
    'departments.search',
    'departments.employees',
    'departments.expand',
    'departments.content',
  ],
}
