/** External correspondents strings: the administration grid, status confirmation, and the
 * create/edit form. Lookup type names and example formats stay out of the catalog. */
export const EXTERNAL_ORGANIZATIONS_LABELS = {
  'external-org.status.active': 'نشط',
  'external-org.status.suspended': 'موقوف',
  'external-org.create': 'جهة خارجية جديدة',
  'external-org.caption': 'قائمة الجهات الخارجية',
  'external-org.search-placeholder': 'ابحث برمز الجهة أو اسمها…',

  'external-org.toast.copied': 'تم نسخ {label}.',
  'external-org.toast.copy-failed': 'تعذّر النسخ إلى الحافظة.',
  'external-org.toast.suspended': 'تم إيقاف الجهة الخارجية.',
  'external-org.toast.activated': 'تم تفعيل الجهة الخارجية.',
  'external-org.copy.code': 'الرمز',
  'external-org.copy.id': 'المعرّف',

  'external-org.column.code': 'الرمز',
  'external-org.column.organization': 'الجهة',
  'external-org.column.type': 'النوع',
  'external-org.column.contact': 'بيانات الاتصال',
  'external-org.column.status': 'الحالة',
  'external-org.column.updated-at': 'آخر تحديث',
  'external-org.column.created-at': 'تاريخ الإنشاء',

  'external-org.menu.edit': 'تعديل البيانات',
  'external-org.menu.suspend': 'إيقاف الجهة',
  'external-org.menu.activate': 'تفعيل الجهة',
  'external-org.menu.copy-code': 'نسخ الرمز',
  'external-org.menu.copy-id': 'نسخ المعرّف',

  'external-org.filter.active': 'الفعّالة فقط',
  'external-org.filter.all': 'الفعّالة والموقوفة',
  'external-org.filter.aria': 'تصفية حسب الحالة',

  'external-org.empty.title': 'لا توجد جهات خارجية بعد.',
  'external-org.empty.description':
    'ابدأ بإضافة أول جهة خارجية لتظهر في اختيار جهات المشاركين.',
  'external-org.empty.search-title': 'لا توجد نتائج مطابقة.',
  'external-org.empty.search-description': 'جرّب تعديل عبارة البحث أو المرشح لعرض سجلات أخرى.',
  'external-org.empty.clear': 'مسح البحث والمرشحات',
  'external-org.empty.add': 'إضافة جهة',

  'external-org.confirm.suspend-title': 'إيقاف الجهة الخارجية',
  'external-org.confirm.activate-title': 'تفعيل الجهة الخارجية',
  'external-org.confirm.suspend-description':
    'ستُعلَّم الجهة «{name}» كموقوفة، ولن تظهر بعدها في اختيار جهات المشاركين.',
  'external-org.confirm.activate-description':
    'ستعود الجهة «{name}» فعّالة ومتاحة للاختيار في المشاركين.',

  'external-org.form.edit-title': 'تعديل الجهة الخارجية',
  'external-org.form.edit-description':
    'حدّث اسم الجهة ونوعها وبيانات الاتصال. لا يمكن تغيير الرمز بعد الإنشاء.',
  'external-org.form.create-description':
    'أضف جهة مراسلة خارجية (وزارة أو شركة أو فرد) لتظهر في اختيار جهات المشاركين.',
  'external-org.form.save': 'حفظ التعديلات',
  'external-org.form.submit': 'إضافة الجهة',
  'external-org.form.toast-updated': 'تم تحديث بيانات الجهة الخارجية.',
  'external-org.form.toast-created': 'تمت إضافة الجهة الخارجية.',
  'external-org.form.code': 'رمز الجهة',
  'external-org.form.type': 'نوع الجهة',
  'external-org.form.type-loading': 'جارٍ تحميل الأنواع…',
  'external-org.form.type-placeholder': 'اختر النوع…',
  'external-org.form.name-ar': 'اسم الجهة بالعربية',
  'external-org.form.name-ar-placeholder': 'مثال: وزارة الداخلية',
  'external-org.form.name-en': 'اسم الجهة بالإنجليزية',
  'external-org.form.contact': 'بيانات الاتصال (بريد إلكتروني أو هاتف)',
  'external-org.form.contact-placeholder': 'name@example.com أو +963 9xx xxx xxx',

  'external-org.validation.code-required': 'رمز الجهة الخارجية مطلوب.',
  'external-org.validation.code-max': 'رمز الجهة الخارجية يتجاوز الحد الأقصى للأحرف.',
  'external-org.validation.name-ar-required': 'اسم الجهة بالعربية مطلوب.',
  'external-org.validation.name-ar-max': 'اسم الجهة بالعربية يتجاوز الحد الأقصى للأحرف.',
  'external-org.validation.type-required': 'اختر نوع الجهة.',
  'external-org.validation.name-en-max': 'اسم الجهة بالإنجليزية يتجاوز الحد الأقصى للأحرف.',
  'external-org.validation.contact-max': 'بيانات الاتصال تتجاوز الحد الأقصى للأحرف.',
  'external-org.validation.contact-format':
    'بيانات الاتصال يجب أن تكون بريدًا إلكترونيًا أو رقم هاتف.',
} as const