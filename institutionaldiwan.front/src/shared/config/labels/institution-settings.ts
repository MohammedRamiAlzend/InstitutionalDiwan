/** Institution settings singleton strings: identity, central diwan, archive diwan, numbering,
 * responsible employee, and the logo upload field. Lookup names and example formats stay out. */
export const INSTITUTION_SETTINGS_LABELS = {
  'institution.status.suspended': 'موقوف',
  'institution.toast.saved': 'تم حفظ إعدادات المؤسسة.',
  'institution.save': 'حفظ الإعدادات',
  'institution.updated-at': 'آخر تحديث:',

  'institution.identity.title': 'بيانات المؤسسة',
  'institution.identity.description':
    'الاسم الرسمي والشعار اللذان يظهران في الواجهة، والمنطقة الزمنية الافتراضية المستخدمة في الوثائق.',
  'institution.name-ar': 'اسم المؤسسة بالعربية',
  'institution.name-ar-hint':
    'يظهر في صفحة الدخول وفي القائمة الجانبية وفي عنوان تبويب المتصفح.',
  'institution.name-ar-placeholder': 'مثال: الديوان المركزي',
  'institution.name-en': 'اسم المؤسسة بالإنجليزية',
  'institution.logo': 'شعار المؤسسة',
  'institution.timezone': 'المنطقة الزمنية الافتراضية',

  'institution.central.title': 'الديوان المركزي',
  'institution.central.description':
    'قسم رئيسي (بلا قسم أعلى) من نوع الديوان المركزي. تغييره يتطلب تسجيل سبب النقل. يمكنك أيضًا قصر إنشاء الوثائق عليه.',
  'institution.central.field': 'قسم الديوان المركزي',
  'institution.central.loading': 'جارٍ تحميل الأقسام…',
  'institution.central.placeholder': 'اختر الديوان المركزي…',
  'institution.transfer.field': 'سبب نقل الديوان المركزي',
  'institution.transfer.placeholder': 'اذكر سبب تغيير قسم الديوان المركزي…',
  'institution.transfer.required': 'سبب نقل الديوان المركزي مطلوب عند تغيير القسم المحدد.',
  'institution.other-create.label': 'السماح للدواوين الأخرى بإنشاء وثائق',
  'institution.other-create.description':
    'عند إيقافه ينشئ الديوان المركزي وحده الوثائق، وتصل الدواوين الأخرى بالإحالة.',

  'institution.archive.title': 'ديوان الحفظ',
  'institution.archive.description':
    'القسم الذي تنتهي إليه الوثائق المحفوظة. أي ديوان يرسل الوثيقة للحفظ إلى الديوان الذي أنشأها، وهو يرسلها إلى ديوان الحفظ، وهناك تُؤرشف.',
  'institution.archive.field': 'قسم ديوان الحفظ',
  'institution.archive.none': 'بلا ديوان حفظ (الأرشفة عند الديوان المنشئ)',
  'institution.archive.note':
    'بدون ديوان حفظ تبقى الأرشفة عند الديوان الذي أنشأ الوثيقة كما هي. الوثائق المؤرشفة قبل تعيينه تبقى عند منشئها.',

  'institution.numbering.title': 'الترقيم',
  'institution.numbering.description': 'النمط الافتراضي لأرقام الوثائق الصادرة وسياسة تصفير التسلسل.',
  'institution.numbering.mode': 'نمط الترقيم',
  'institution.numbering.loading': 'جارٍ التحميل…',
  'institution.numbering.mode-placeholder': 'اختر النمط…',
  'institution.numbering.reset': 'سياسة التصفير',
  'institution.numbering.reset-placeholder': 'اختر السياسة…',
  'institution.numbering.reset-aria': 'سياسة تصفير الترقيم',

  'institution.responsible.title': 'الجهة المسؤولة',
  'institution.responsible.description': 'الموظف المسؤول عن متابعة إعدادات المؤسسة والترقيم.',
  'institution.responsible.field': 'الموظف المسؤول',
  'institution.responsible.none': 'بدون موظف مسؤول',
  'institution.responsible.denied':
    'لا تملك صلاحية إدارة الموظفين، لذلك لا يمكنك تغيير الموظف المسؤول.',

  'institution.logo.invalid-type': 'الملف ليس صورة مقبولة. اختر PNG أو JPEG أو WebP أو SVG.',
  'institution.logo.too-large': 'حجم الشعار يتجاوز الحد الأقصى (1 ميغابايت).',
  'institution.logo.toast-uploaded': 'تم تحديث شعار المؤسسة.',
  'institution.logo.toast-removed': 'تمت إزالة شعار المؤسسة.',
  'institution.logo.replace': 'استبدال الشعار',
  'institution.logo.upload': 'رفع شعار',
  'institution.logo.remove': 'إزالة الشعار',
  'institution.logo.input-aria': 'ملف شعار المؤسسة',
  'institution.logo.hint':
    'PNG أو JPEG أو WebP أو SVG، حتى 1 ميغابايت. يظهر الشعار في صفحة الدخول وفي القائمة الجانبية وفي أيقونة التبويب، وتُستحسن الصورة المربعة بخلفية شفافة.',

  'institution.validation.name-ar-required': 'اسم المؤسسة بالعربية مطلوب.',
  'institution.validation.name-ar-max': 'اسم المؤسسة بالعربية يتجاوز الحد الأقصى للأحرف.',
  'institution.validation.name-en-max': 'اسم المؤسسة بالإنجليزية يتجاوز الحد الأقصى للأحرف.',
  'institution.validation.timezone-required': 'المنطقة الزمنية الافتراضية مطلوبة.',
  'institution.validation.timezone-max': 'المنطقة الزمنية تتجاوز الحد الأقصى للأحرف.',
  'institution.validation.mode-required': 'اختر نمط الترقيم.',
  'institution.validation.reset-required': 'اختر سياسة تصفير الترقيم.',
  'institution.validation.central-required': 'اختر قسم الديوان المركزي.',
  'institution.validation.transfer-max': 'سبب النقل يتجاوز الحد الأقصى للأحرف.',
} as const