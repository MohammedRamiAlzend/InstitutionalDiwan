/** Module 03 routing area: the movements page tabs, the department inbox/outbox
 * queue (columns, filters, row menus, empty states), the routing action dialogs
 * (open/send/receive/complete/close/reject/return/redirect/cancel) and the
 * movement timeline note templates. Data-driven text (document titles,
 * department/type/status names, schema field labels) stays out of the catalog —
 * it is editable from its own admin screens. */
export const MOVEMENT_LABELS = {
  'movements.tab.inbox': 'الوارد',
  'movements.tab.inbox-hint': 'حركات موجهة إلى قسمك المبدئي',
  'movements.tab.outbox': 'الصادر',
  'movements.tab.outbox-hint': 'حركات أرسلها قسمك المبدئي',
  'movements.tabs-aria': 'صناديق الحركات',

  'movements.search-placeholder': 'ابحث باسم الوثيقة أو رقمها أو التعليمات…',

  'movements.filter.all-statuses': 'كل الحالات',
  'movements.filter.all-types': 'كل الأنواع',
  'movements.filter.status-aria': 'تصفية حسب حالة الحركة',
  'movements.filter.type-aria': 'تصفية حسب نوع الحركة',
  'movements.filter.clear': 'مسح المرشحات',

  'movements.column.document': 'الوثيقة',
  'movements.column.type': 'نوع الحركة',
  'movements.column.status': 'حالة الحركة',
  'movements.column.step': 'الخطوة',
  'movements.column.route': 'المسار',
  'movements.column.due-date': 'تاريخ الاستحقاق',
  'movements.column.sent-at': 'تاريخ الإرسال',
  'movements.column.assignee': 'المكلَّف',
  'movements.column.instructions': 'التعليمات',
  'movements.column.created-at': 'تاريخ الإنشاء',

  'movements.step.prefix': 'الخطوة',
  'movements.route.from': 'من:',
  'movements.route.to': 'إلى:',
  'movements.overdue': 'متأخر',

  'movements.row.open': 'فتح الوثيقة',
  'movements.row.copy-number': 'نسخ رقم النظام',
  'movements.row.copy-id': 'نسخ معرّف الحركة',
  'movements.copy.number-label': 'رقم النظام',
  'movements.copy.id-label': 'معرّف الحركة',
  'movements.toast.copied': 'تم نسخ {label}.',
  'movements.toast.copy-failed': 'تعذّر النسخ إلى الحافظة.',

  'movements.action.refresh': 'تحديث الصندوق',
  'movements.action.send': 'إرسال',
  'movements.action.receive': 'استلام',
  'movements.action.complete': 'إكمال',
  'movements.action.reject': 'رفض',
  'movements.action.return': 'إرجاع',
  'movements.action.redirect': 'إعادة توجيه',
  'movements.action.close': 'إغلاق',
  'movements.action.cancel': 'إلغاء الحركة',

  'movements.caption.inbox': 'صندوق وارد القسم',
  'movements.caption.outbox': 'صندوق صادر القسم',

  'movements.empty.no-match-title': 'لا توجد حركات مطابقة.',
  'movements.empty.none-title': 'الصندوق فارغ.',
  'movements.empty.no-match-description': 'جرّب تعديل البحث أو المرشحات لعرض حركات أخرى.',
  'movements.empty.inbox-description': 'ستظهر هنا الحركات الموجهة إلى قسمك المبدئي فور إرسالها.',
  'movements.empty.outbox-description':
    'ستظهر هنا الحركات التي أرسلها قسمك المبدئي ولم تُغلق بعد.',
  'movements.empty.clear-query': 'مسح البحث والمرشحات',

  'movements.loading': 'جارٍ التحميل…',
  'movements.destination-placeholder': 'اختر القسم…',
  'movements.field.instructions': 'تعليمات',
  'movements.field.reason': 'السبب',
  'movements.field.movement-type': 'نوع الحركة',
  'movements.field.due-date': 'تاريخ الاستحقاق',
  'movements.field.assignee': 'المكلَّف',
  'movements.assignee.none': 'بدون إسناد',
  'movements.reason-placeholder': 'اكتب سببًا واضحًا يظهر في سجل الحركة…',

  'movements.open.title': 'فتح حركة',
  'movements.open.description': 'إرسال «{title}» من قسمك الأساسي إلى قسم آخر مباشرةً.',
  'movements.open.central-hint':
    'الديوان المركزي إلزامي: يمر كل توجيه عبر الديوان المركزي ما لم يكن قسمك أو القسم الوجهة هو الديوان المركزي. تُرسل الحركة فورًا عند الحفظ، ولا يمكن الاستلام قبل أن يسجّل القسم الوجهة رقمه في سجلّه.',
  'movements.open.field-destination': 'القسم الوجهة',
  'movements.open.assignee-search-label': 'البحث عن مكلَّف (اختياري)',
  'movements.open.assignee-search-placeholder': 'ابحث باسم المستخدم…',
  'movements.open.instructions-placeholder': 'إرشادات اختيارية للقسم الوجهة…',
  'movements.open.auto-cancel-reason': 'تعذّر إرسال الحركة؛ أُلغيت المسودة تلقائيًا.',
  'movements.open.toast': 'أُرسلت الحركة إلى القسم الوجهة.',

  'movements.send.description':
    'سيُعلَّم القسم الوجهة بأن الوثيقة في طريقه؛ تبقى الوثيقة قيد التداول حتى الإغلاق.',
  'movements.send.notes-label': 'ملاحظات الإرسال (اختياري)',
  'movements.send.notes-placeholder': 'ملاحظة اختيارية تُرفق بالإرسال…',
  'movements.send.submit': 'إرسال الحركة',
  'movements.send.toast': 'تم إرسال الحركة إلى القسم الوجهة.',

  'movements.complete.description': 'تُغلق الخطوة الحالية كمكتملة، وتبقى الحركة مفتوحة حتى تُغلق.',
  'movements.complete.notes-label': 'ملاحظات النتيجة (اختياري)',
  'movements.complete.notes-placeholder': 'نتيجة المعالجة في هذا القسم…',
  'movements.complete.submit': 'إكمال الخطوة',
  'movements.complete.toast': 'اكتملت الخطوة الحالية للحركة.',

  'movements.close.description':
    'يُغلق سجل الحركة وتعود الوثيقة إلى حالة مستقرة قابلة للتوجيه من جديد أو للأرشفة.',
  'movements.close.notes-label': 'ملاحظات الإغلاق (اختياري)',
  'movements.close.notes-placeholder': 'ملاحظة ختامية عن الحركة…',
  'movements.close.submit': 'إغلاق الحركة',
  'movements.close.toast': 'أُغلقت الحركة وأُطلقت الوثيقة.',

  'movements.reject.description':
    'الرفض يتطلب سببًا. إذا لم يكن قسمك هو الديوان المركزي فستُعاد الوثيقة إليه في خطوة جديدة.',
  'movements.reject.submit': 'رفض الحركة',
  'movements.reject.toast': 'تم رفض الحركة.',

  'movements.return.description': 'إرجاع الوثيقة إلى القسم المُرسل يتطلب سببًا، وتُعرض الحالة «مُعاد».',
  'movements.return.submit': 'إرجاع الوثيقة',
  'movements.return.toast': 'أُعيدت الوثيقة إلى القسم المُرسل.',

  'movements.cancel.description':
    'إلغاء الحركة نهائي ولا يمكن التراجع عنه، ويتطلب سببًا يظهر في سجل الحركة.',
  'movements.cancel.toast': 'أُلغيت الحركة.',

  'movements.redirect.description': 'تُضاف خطوة جديدة وتُرسل مباشرة إلى القسم الجديد دون إغلاق الحركة.',
  'movements.redirect.submit': 'إعادة التوجيه',
  'movements.redirect.central-hint':
    'يجب أن يكون الديوان المركزي على أحد طرفي التوجيه الجديد؛ لا تنتقل الوثيقة مباشرة بين قسمين عاديين.',
  'movements.redirect.field-destination': 'القسم الوجهة الجديد',
  'movements.redirect.instructions-placeholder': 'إرشادات اختيارية للقسم الجديد…',
  'movements.redirect.toast': 'أُعيد توجيه الحركة إلى القسم الجديد.',

  'movements.receive.description':
    'يُكتب الاستلام في سجل الدائرة الوجهة: يبقى رقم الوثيقة نفسه بتاريخ جديد في كل مرة تصل فيها، إلا إذا سُجّلت برقم جديد.',
  'movements.receive.submit': 'استلام الوثيقة',
  'movements.receive.field-registry': 'السجل',
  'movements.receive.field-number': 'رقم سجل الدائرة',
  'movements.receive.field-number-new': 'رقم سجل الدائرة الجديد',
  'movements.receive.field-date': 'تاريخ التسجيل',
  'movements.receive.revisit-hint': 'زيارة متكررة، الرقم نفسه بتاريخ جديد.',
  'movements.receive.number-optional-hint':
    'اتركه فارغًا إذا كان لدائرتك رقم لهذه الوثيقة في هذه السنة.',
  'movements.receive.replaces-prefix': 'يحلّ محلّ الرقم',
  'movements.receive.replaces-suffix': 'الذي يبقى في السجل مستبدَلًا ومرتبطًا بالرقم الجديد.',
  'movements.receive.new-number-label': 'تسجيل الوثيقة برقم جديد',
  'movements.receive.new-number-description':
    'للدائرة رقم سابق لهذه الوثيقة في هذه السنة؛ الرقم الجديد يصبح المعتمد ويبقى مرتبطًا بالسابق.',
  'movements.receive.record-section-aria': 'سجل الديوان',
  'movements.receive.record-title': 'سجل الديوان',
  'movements.receive.record-hint':
    'الحقول الإلزامية تُملأ الآن، والباقي يُكمَّل لاحقًا ما دامت الوثيقة لدى الديوان.',
  'movements.receive.notes-label': 'ملاحظات الاستلام (اختياري)',
  'movements.receive.notes-placeholder': 'ملاحظة اختيارية تُرفق بالاستلام…',
  'movements.receive.error.registry-required': 'اختر السجل الذي سُجّل فيه الرقم.',
  'movements.receive.error.number-required': 'أدخل رقم سجل الدائرة لهذه السنة.',
  'movements.receive.error.number-same': 'الرقم الجديد يطابق الرقم الحالي.',
  'movements.receive.toast.replaced':
    'تم الاستلام برقم الدائرة الجديد {number} بدل الرقم {replaced}.',
  'movements.receive.toast.return': 'تم الاستلام بنفس رقم الدائرة {number} وبتاريخ جديد.',
  'movements.receive.toast.normal': 'تم استلام الوثيقة برقم الدائرة {number}.',

  'movements.archive-hint':
    'الوثيقة المرسلة للحفظ تذهب إلى الديوان الذي أنشأها، وهو وحده يرسلها إلى ديوان الحفظ. اختر الوجهة على هذا الأساس.',

  'movements.note.routed-to-central': 'أُعيد توجيه الوثيقة إلى الديوان المركزي بعد الرفض: {detail}',
  'movements.note.rejected': 'مرفوض: {detail}',

  'movements.validation.date-invalid': 'التاريخ غير صالح.',
  'movements.validation.instructions-max': 'التعليمات تتجاوز الحد الأقصى للأحرف.',
  'movements.validation.destination-required': 'اختر القسم الوجهة.',
  'movements.validation.movement-type-required': 'اختر نوع الحركة.',
  'movements.validation.notes-max': 'الملاحظات تتجاوز الحد الأقصى للأحرف.',
  'movements.validation.registry-date-required': 'تاريخ التسجيل مطلوب.',
  'movements.validation.registry-number-max': 'رقم سجل الدائرة يتجاوز الحد الأقصى للأحرف.',
  'movements.validation.reason-required': 'السبب مطلوب.',
  'movements.validation.reason-max': 'السبب يتجاوز الحد الأقصى للأحرف.',
  'movements.validation.new-destination-required': 'اختر القسم الوجهة الجديد.',
} as const