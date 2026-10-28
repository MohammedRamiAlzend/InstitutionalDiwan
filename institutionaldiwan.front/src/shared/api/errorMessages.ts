import { ApiError } from './problemDetails'

/**
 * Arabic user-facing messages for API failures. The server sends English
 * `detail` text, so the UI always renders the Arabic message for the error's
 * domain code (`ApiError.code`), falling back to the HTTP status, then to a
 * generic Arabic message. Extend `CODE_MESSAGES` module by module.
 */
const CODE_MESSAGES: Record<string, string> = {
  // Module 01 — Identity
  'Identity.InvalidCredentials': 'اسم المستخدم أو كلمة المرور غير صحيحة.',
  'Identity.NotAuthenticated': 'لم يتم التحقق من هويتك، يرجى تسجيل الدخول من جديد.',
  'Identity.UserInactive': 'هذا الحساب موقوف، تواصل مع مشرف النظام.',
  'Identity.UsernameTaken': 'اسم المستخدم مستخدم مسبقًا.',
  'Identity.RoleNotFound': 'الدور المحدد غير موجود.',
  'Identity.RoleCodeAlreadyExists': 'يوجد دور آخر بنفس الرمز.',
  'Identity.PermissionNotFound': 'الصلاحية المحددة غير موجودة.',
  'Identity.UserNotFound': 'حساب المستخدم غير موجود.',
  'Identity.CurrentPasswordIncorrect': 'كلمة المرور الحالية غير صحيحة.',
  'Identity.NewPasswordSameAsOld': 'يجب أن تختلف كلمة المرور الجديدة عن الحالية.',
  'Identity.EmployeeNotFound': 'سجل الموظف غير موجود.',
  'Identity.AccessGrantNotFound': 'تصريح الوصول إلى الوثيقة غير موجود.',
  'Identity.RoleAlreadyAssigned': 'هذا الدور مُسند مسبقًا إلى المستخدم.',
  'Identity.RoleNotAssigned': 'هذا الدور غير مُسند إلى المستخدم.',
  'Identity.SystemRoleProtected': 'الأدوار المعرفة من النظام لا يمكن إيقافها أو حذفها.',
  'Identity.PermissionAlreadyGranted': 'هذه الصلاحية ممنوحة مسبقًا لهذا الدور.',
  'Identity.EmployeeNameEmpty': 'اسم الموظف الكامل بالعربية مطلوب.',
  'Identity.EmployeeEmailInvalid': 'صيغة البريد الإلكتروني غير صحيحة.',
  'Identity.EmployeeDepartmentEmpty': 'قسم الموظف مطلوب.',
  'Identity.InvalidDateRange': 'تاريخ الانتهاء لا يمكن أن يسبق تاريخ البدء.',
  'Identity.AccessLevelInvalid': 'مستوى الوصول يجب أن يكون عرضًا أو تنزيلًا أو إحالةً أو إدارةً.',
  'Identity.GrantAlreadyRevoked': 'تم إلغاء تصريح الوصول إلى الوثيقة مسبقًا.',
  'Identity.DepartmentAlreadyAssigned': 'للمستخدم إسناد فعّال على هذا القسم مسبقًا.',
  'Identity.DepartmentAssignmentNotFound': 'إسناد القسم غير موجود لهذا المستخدم.',
  'Identity.AssignmentTypeInvalid': 'نوع الإسناد المحدد غير موجود أو موقوف.',
  'Identity.UsernameEmpty': 'اسم المستخدم مطلوب.',
  'Identity.UsernameTooShort': 'اسم المستخدم قصير جدًا، الحد الأدنى 3 أحرف.',
  'Identity.PasswordEmpty': 'كلمة المرور مطلوبة.',
  'Identity.PasswordTooShort': 'كلمة المرور قصيرة جدًا، الحد الأدنى 8 أحرف.',
  'Identity.UserScopeTypeInvalid': 'نطاق المستخدم المحدد غير صالح.',
  'Identity.PasswordHashEmpty': 'كلمة المرور مطلوبة.',
  'Identity.RoleCodeEmpty': 'رمز الدور مطلوب.',
  'Identity.RoleNameEmpty': 'الاسم العربي للدور مطلوب.',
  'Identity.PermissionCodeEmpty': 'رمز الصلاحية مطلوب.',
  'Identity.ModuleNameEmpty': 'اسم وحدة الصلاحية مطلوب.',

  // Departments and lookups (used by identity pickers)
  'Department.CodeEmpty': 'رمز القسم مطلوب.',
  'Department.CodeTooLong': 'رمز القسم يتجاوز الحد الأقصى للأحرف.',
  'Department.NameEmpty': 'اسم القسم بالعربية مطلوب.',
  'Department.TypeEmpty': 'نوع القسم مطلوب.',
  'Department.SelfParent': 'لا يمكن أن يكون القسم تابعًا لنفسه.',
  'Department.CyclicHierarchy': 'لا يمكن نقل القسم تحت أحد أقسامه الفرعية.',
  'Department.InactiveParent': 'لا يمكن نقل القسم تحت قسم رئيسي موقوف.',
  'Department.HasActiveChildren': 'لا يمكن إيقاف قسم لديه أقسام فرعية فعّالة.',
  'Department.NotFound': 'القسم المحدد غير موجود.',
  'Department.CodeAlreadyExists': 'يوجد قسم آخر بنفس الرمز.',
  'Department.TypeNotFound': 'نوع القسم المحدد غير موجود.',
  'Department.TypeInactive': 'لا يمكن إسناد نوع قسم موقوف.',
  'Department.ParentNotFound': 'القسم الرئيسي المحدد غير موجود.',
  'Department.Inactive': 'القسم موقوف ولا يمكن استخدامه.',
  'Lookup.InvalidId': 'معرّف القيمة المرجعية غير صالح.',
  'Lookup.LabelEmpty': 'القيمة المرجعية تتطلب تسمية عربية وإنجليزية.',
  'Lookup.UnknownCatalog': 'جدول القيم المرجعية المطلوب غير مدعوم.',
  'Lookup.EntryNotFound': 'القيمة المرجعية المطلوبة غير موجودة.',
  'Lookup.EntryInactive': 'القيمة المرجعية موقوفة ولا يمكن اختيارها.',
  'Lookup.EntryInUse': 'القيمة المرجعية مستخدمة في سجلات أخرى ولا يمكن إيقافها.',
  'Lookup.CatalogReadOnly': 'لا يمكن إضافة قيم إلى هذا الجدول المرجعي من الشاشة العامة.',
  'Lookup.IdAlreadyExists': 'يوجد عنصر آخر بنفس المعرّف في هذا الجدول المرجعي.',
}

const STATUS_MESSAGES: Record<number, string> = {
  400: 'البيانات المُدخلة غير صالحة، راجع الحقول وحاول مجددًا.',
  401: 'الرجاء تسجيل الدخول للمتابعة.',
  403: 'لا تملك صلاحية تنفيذ هذا الإجراء.',
  404: 'العنصر المطلوب غير موجود.',
  409: 'تعارض مع حالة حالية، حدّث البيانات وحاول مجددًا.',
  422: 'تعذّر معالجة البيانات المُرسلة.',
  500: 'حدث خطأ في الخادم، حاول لاحقًا.',
}

const GENERIC_MESSAGE = 'حدث خطأ غير متوقع، حاول مجددًا.'
const NETWORK_MESSAGE = 'تعذّر الاتصال بالخادم، تحقق من الشبكة وحاول مجددًا.'

/** Always returns an Arabic message suitable for rendering to the user. */
export function toArabicErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 0) return NETWORK_MESSAGE
    return CODE_MESSAGES[error.code] ?? STATUS_MESSAGES[error.status] ?? GENERIC_MESSAGE
  }
  return NETWORK_MESSAGE
}
