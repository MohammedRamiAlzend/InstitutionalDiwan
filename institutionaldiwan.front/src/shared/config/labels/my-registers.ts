/** `/my-registers` — the caller's own department registers browser. The page
 * header already renders from `page.my-registers.*`; this catalog covers the
 * table: status filter, register/form/version pickers, columns, linked-number
 * cells, row menus, copy toasts, filters and the layered empty states. The
 * dynamic form-field columns keep `field.labelAr`, and `FieldFilterBar` belongs
 * to the document-schema area. Data-driven text (register/document/type names)
 * stays out. */
export const MY_REGISTERS_LABELS = {
  'my-registers.caption': 'سجلاتي',
  'my-registers.search-placeholder': 'ابحث برقم السجل أو الوثيقة أو رقم الديوان أو قيم النموذج…',

  'my-registers.status.all': 'كل الحالات',
  'my-registers.status.active': 'نشط',
  'my-registers.status.replaced': 'مُستبدل',
  'my-registers.status.cancelled': 'ملغى',

  'my-registers.registry.suspended': '(موقوف)',
  'my-registers.type.no-form': 'بلا بيانات نموذج',
  'my-registers.type.fallback': 'نوع {id}',
  'my-registers.version.label': 'الإصدار {number}',
  'my-registers.version.newest': '(الأحدث)',
  'my-registers.version.retired': 'متقاعد',

  'my-registers.column.registry-number': 'رقم السجل',
  'my-registers.column.registry-date': 'تاريخ التسجيل',
  'my-registers.column.document': 'الوثيقة',
  'my-registers.column.central-number': 'رقم الديوان',
  'my-registers.column.linked-number': 'الرقم المرتبط',
  'my-registers.column.year': 'السنة',
  'my-registers.column.status': 'الحالة',
  'my-registers.column.registration-count': 'مرات التسجيل',

  'my-registers.linked.replaced-by': 'استُبدل بـ',
  'my-registers.linked.replaces': 'بدل',

  'my-registers.row.open': 'فتح الوثيقة',
  'my-registers.row.copy-registry': 'نسخ رقم السجل',
  'my-registers.row.copy-central': 'نسخ رقم الديوان',
  'my-registers.row.copy-id': 'نسخ معرّف الوثيقة',
  'my-registers.copy.document-id': 'معرّف الوثيقة',
  'my-registers.toast.copied': 'تم نسخ {label}.',
  'my-registers.toast.copy-failed': 'تعذّر النسخ إلى الحافظة.',

  'my-registers.filter.registry-aria': 'اختيار السجل',
  'my-registers.filter.registry-loading': 'جارٍ التحميل…',
  'my-registers.filter.registry-placeholder': 'اختر السجل…',
  'my-registers.filter.form-aria': 'نموذج سجل القسم',
  'my-registers.filter.version-aria': 'إصدار النموذج',
  'my-registers.filter.year-aria': 'تصفية حسب سنة الترقيم',
  'my-registers.filter.year-placeholder': 'كل السنوات',
  'my-registers.filter.status-aria': 'تصفية حسب حالة الرقم',
  'my-registers.filter.from-aria': 'من تاريخ',
  'my-registers.filter.to-aria': 'إلى تاريخ',
  'my-registers.filter.clear': 'مسح المرشحات',

  'my-registers.empty.no-registers-title': 'لا سجلات لقسمك',
  'my-registers.empty.choose-title': 'اختر سجلًا لعرض أسطره',
  'my-registers.empty.no-match-title': 'لا توجد نتائج مطابقة.',
  'my-registers.empty.no-version-lines-title': 'لا أسطر على هذا الإصدار.',
  'my-registers.empty.no-lines-title': 'لا أسطر في هذا السجل بعد.',
  'my-registers.empty.no-registers-description': 'لم يُنشأ لقسمك سجل ترقيم. يُنشئه مسؤول الترقيم من صفحة «الترقيم».',
  'my-registers.empty.choose-description': 'تظهر هنا الأسطر المسجّلة في السجل الذي تختاره من سجلات قسمك.',
  'my-registers.empty.no-match-description': 'جرّب تعديل البحث أو المرشحات، أو امسحها لعرض كل أسطر السجل.',
  'my-registers.empty.no-version-lines-description': 'لا أسطر لها زيارة مسجّلة على هذا الإصدار من النموذج. جرّب إصدارًا آخر أو «بلا بيانات نموذج».',
  'my-registers.empty.no-lines-description': 'تُضاف الأسطر عند تسجيل وثيقة في هذا السجل أو استلامها.',
  'my-registers.empty.clear-search': 'مسح البحث',
} as const