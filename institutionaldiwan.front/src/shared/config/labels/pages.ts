/** Static page titles and descriptions rendered through `PageHeader`. Dynamic titles
 * (a document's subject, a person's name) and feature-flag-composed descriptions stay in code. */
export const PAGE_LABELS = {
  'page.dashboard.title': 'لوحة التحكم',
  'page.dashboard.description': 'نظرة سريعة على حسابك وما هو متاح في مساحة العمل.',
  'page.documents.title': 'الوثائق',
  'page.movements.title': 'الحركات',
  'page.movements.description':
    'تابع الوثائق المرسلة إلى قسمك والمرسلة منه. يُعرض الصندوقان لقسمك المبدئي؛ تفتح الحركة وتُدار من صفحة الوثيقة عند الحاجة.',
  'page.my-registers.title': 'سجلاتي',
  'page.my-registers.description':
    'اختر أحد سجلات قسمك لتتصفّح أسطره: ابحث برقم السجل أو الوثيقة، وصفِّ بالسنة والحالة والتاريخ، واعرض بيانات نموذج سجل القسم حسب إصداره.',
  'page.decisions.title': 'القرارات',
  'page.decisions.description':
    'القرارات الصادرة على الوثائق التي تملك الاطلاع عليها، وأنواع القرارات التي يُعدّها مدير القوائم.',
  'page.archive.title': 'الأرشيف',
  'page.archive.description':
    'الوثائق المؤرشفة التي يحق لك الاطلاع عليها. الوثيقة المؤرشفة مجمّدة أمام التعديل حتى تُلغى أرشفتها من صفحة تفاصيلها.',
  'page.notifications.title': 'الإشعارات',
  'page.notifications.description':
    'تنبيهات الوثائق والإحالات الموجّهة إليك أو إلى قسمك. تصل لحظيًا عند اتصال التحديث الفوري، وإلا فتُحدَّث القائمة عند فتح الصفحة.',
  'page.persons.title': 'الأشخاص',
  'page.persons.description':
    'المدعى عليهم والمدعون وغيرهم ممن وردت أسماؤهم في الوثائق. ابحث بالاسم وافتح ملف الشخص لتتابع وثائقه.',
  'page.users.title': 'إدارة المستخدمين',
  'page.users.description':
    'أنشئ حسابات المستخدمين، وحدّث أقسامهم ونطاقات وصولهم، وأسند الأدوار وإسنادات الأقسام.',
  'page.roles.title': 'الأدوار والصلاحيات',
  'page.roles.description':
    'أنشئ الأدوار المؤسسية وحدّث بياناتها، وحدد الصلاحيات الممنوحة لكل دور من مصفوفة الصلاحيات.',
  'page.departments.title': 'الأقسام التنظيمية',
  'page.departments.description':
    'إدارة الهيكل التنظيمي والأقسام ومديريها، مع عرض الموظفين والشجرة والمخططات والجدول.',
  'page.employees.title': 'إدارة الموظفين',
  'page.employees.description':
    'أضف سجلات الموظفين واربطها بالأقسام، وحدّث بياناتهم الوظيفية وحالة تفعيلهم.',
  'page.lookups.title': 'الجداول المرجعية',
  'page.lookups.description':
    'القيم التي تظهر في القوائم المنسدلة عبر النظام. أي تغيير يسري فورًا على بقية الشاشات.',
  'page.document-schemas.title': 'مخططات الحقول',
  'page.document-schemas.description':
    'حقول إضافية لكل نوع وثيقة تظهر في نموذج الإنشاء. كل تغيير يمرّ عبر نسخة جديدة تُنشر ولا تؤثر في الوثائق الموجودة.',
  'page.numbering.title': 'الترقيم',
  'page.numbering.description':
    'أنشئ سجلات الترقيم وحدّد نطاقها وقالب أرقامها وسياسة تصفيرها، وتابع العدادات وسجل أرقام الدوائر. تُحفظ كل الأرقام الصادرة ولا تُحذف.',
  'page.institution-settings.title': 'إعدادات المؤسسة',
  'page.institution-settings.description':
    'إدارة بيانات المؤسسة والديوان المركزي وسياسة الترقيم والموظف المسؤول.',
  'page.audit.title': 'سجل التدقيق',
  'page.audit.description':
    'سلسلة تدقيق غير قابلة للتعديل: كل إجراء يُخزَّن مع تجزئة تربطه بالسجل السابق. ابحث في السجلات وفُتش تكامل السلسلة عند الحاجة؛ كل عملية بحث تُسجَّل هي أيضًا.',
  'page.system-errors.title': 'الأخطاء البرمجية',
  'page.system-errors.description':
    'كل خطأ يقع في الخادم أو في متصفح المستخدمين، مجمَّعًا بحيث يكون الخطأ المتكرر صفًا واحدًا. تابع حالته وصدّره إلى Excel للعمل عليه.',
} as const