# خطة تصفية سجل الوثائق بقيم الحقول الديناميكية (خلفية + واجهة)

> **الحالة: منفّذة (2026-11-04) — `dotnet test` 776 ناجح، و`npm run build` و`npm run lint` ناجحان، وجُرّبت المرشحات على API حي ومن المتصفح (نص، رقم، مرشحان معًا، أخطاء التحقق، ترميز المعامل المتكرر). لم يُجرَّب: تغيير النوع من القائمة لمسح المرشحات، الوضع الداكن والجوال. اختلافات: لا يُتحقق من وجود مفتاح الخيار (يطابق لا شيء)، ومقارنة decimal عملت في SQLite.** ~~مسودة للمراجعة. لا كود حتى الاعتماد.~~ الخطة تشمل الخلفية والواجهة معًا، لأن التصفية غير منفّذة في أي منهما (`knowledge.md`: «Registry filtering by field values is not implemented yet»).

## 1. الهدف

أن يتمكن مستخدم السجل `/documents` من اختيار **نوع وثيقة** ثم تصفية الوثائق بقيم حقوله الإضافية (مثل: رقم العقد يحتوي «A-12»، المبلغ أكبر من 5000، تاريخ الانتهاء قبل 2027-01-01، «عاجل = نعم»، حالة التنفيذ = «قيد الدراسة»)، مع بقاء كل التصفيات في الرابط وقابلة للمشاركة.

القرار المعتمد سابقًا (`plans/document-dynamic-fields-backend.md`، القرار 2) أن التصفية مطلوبة، وأن التخزين الهجين بأعمدة مكتوبة هو سبب وجوده. والمفتوح هو شكل الاستعلام (النقطة 7.3 هناك)؛ تحسمه هذه الخطة.

## 2. الحالة الراهنة

| العنصر | الحالة |
| --- | --- |
| تخزين القيم | ✅ `DocumentFieldValue` بأعمدة `TextValue` و`NumberValue` (decimal) و`DateValue` و`BoolValue` و`OptionKey` |
| الفهارس | ✅ فهارس مرشّحة (sparse) جاهزة لـ Number وDate وOption وBool، تبدأ بـ `DocumentFieldId`. لا فهرس للنص (`TextValue`) |
| تعريف الحقل | `DocumentField` لكل نوع وثيقة: `Key` فريد ضمن النوع، و`DataType` ثابت لا يتغير (1 نص، 2 نص طويل، 3 رقم، 4 تاريخ، 5 منطقي، 6 اختيار) |
| استعلام السجل | `GET /api/documents` بمرشحات `statusId` و`owningDepartmentId` و`directionId` و`documentTypeId` والبحث (`DocumentRepository.ListPagedAsync` يطبق `WhereViewableBy` أولًا) |
| مرشح الحقول | ❌ لا في `ListDocumentsQuery` ولا المستودع ولا المتحكم |
| الواجهة | `DocumentRegistryTable` يحمل `documentTypeId` في الرابط. `GET /api/document-types/{id}/schema` (المخطط المنشور) موجود وتستعمله `entities/document-schema` |
| ترميز المصفوفات في axios | ⚠️ لا يوجد `paramsSerializer`، والافتراضي يرسل `fieldFilters[]=` الذي لا يُربط في ASP.NET |

## 3. القرارات

1. **شكل المرشح:** معامل متكرر `fieldFilters=key:op:value`. الفاصل `:` يُقسَّم عند أول نقطتين فقط، فتبقى القيم التي تحوي `:` (كالتواريخ ISO) سليمة؛ فمفتاح الحقل لا يحوي إلا `[a-z0-9_]` والمعامل من قائمة ثابتة.
2. **المعاملات حسب النوع:**

| نوع الحقل | المعاملات |
| --- | --- |
| نص، نص طويل | `eq` (لا يميّز حالة الأحرف بحسب collation) و`contains` |
| رقم | `eq` و`gt` و`gte` و`lt` و`lte` |
| تاريخ | `eq` (اليوم كاملًا) و`gt` و`gte` و`lt` و`lte` — النطاق = مرشحان |
| منطقي | `eq` (`true`/`false`) |
| اختيار | `eq` (مفتاح الخيار `OptionKey`) |

   أضفتُ `gte`/`lte` على ما اقترحته الخطة القديمة (`eq/contains/gt/lt`) لأن النطاق الشامل هو الاستعمال الشائع للتواريخ والمبالغ.
3. **المنطق:** AND بين المرشحات. الحد الأقصى **5 مرشحات** في الطلب.
4. **يشترط `documentTypeId`:** المفتاح فريد ضمن النوع فقط، فلا معنى له بدون نوع.
5. **التطابق:** وثيقة تطابق المرشح إذا وُجد لها صف في `DocumentFieldValue` لهذا الحقل يحقق الشرط (`EXISTS`). وثيقة بنسخة مخطط قديمة لا تحوي الحقل لا تطابق، وهذا سلوك مقصود.
6. **الأمان:** تُطبّق المرشحات بعد `WhereViewableBy` فلا تتسرب وثيقة لا يراها المستخدم، ولا يتغير `totalCount` إلا ضمن المرئي.
7. **لا ترحيل (migration):** الفهارس موجودة. قد يُقرَّر لاحقًا فهرس للنص إن ظهر بطء (القسم 7).
8. **لا صلاحية جديدة:** تبقى 17.
9. **النطاق:** `GET /api/documents` فقط؛ سجل الدائرة `/api/departments/{id}/documents` يشارك المستودع نفسه لكن لا يُكشف فيه المعامل الآن (القسم 7).

## 4. المرحلة ب — الخلفية

**الملفات:**
- `Domain/Repositories/IDomainRepositories.cs`: نوع `DocumentFieldFilter(Guid DocumentFieldId, DocumentFieldDataType DataType, FieldFilterOperator Operator, string? Text, decimal? Number, DateTime? Date, bool? Boolean, string? OptionKey)`، وتوقيع `ListPagedAsync` يقبل `IReadOnlyList<DocumentFieldFilter>? fieldFilters`.
- `Application/.../Documents/ListDocumentsQuery.cs`: خاصية `IReadOnlyList<string>? FieldFilters`.
- `Application/.../ListDocumentsQueryHandler.cs` (أو خدمة تحليل صغيرة بجانبه): يحلّل النصوص، ويجلب حقول النوع عبر `IDocumentFieldRepository.ListByDocumentTypeAsync`، ويتحقق من المعامل مقابل `DataType` ومن القيمة (رقم invariant، تاريخ ISO، منطقي، وجود مفتاح الخيار ضمن خيارات الحقل)، ثم يمرّر `DocumentFieldFilter`.
- `Infrastructure/.../DocumentRepository.cs`: لكل مرشح `query.Where(d => _context.DocumentFieldValues.Any(v => v.DocumentId == d.DocumentId && v.DocumentFieldId == f.Id && <شرط العمود>))`. النص `contains` بـ `EF.Functions.Like` مع هروب `%` و`_` و`\` (على نمط `EscapeLike` في `NotificationRepository`، ويُفضَّل استخراجه إلى مساعد مشترك بدل نسخه). `eq` للتاريخ يتحول إلى نطاق `[يوم, يوم+1)`.
- `Server/.../DocumentsController.cs`: `[FromQuery] string[]? fieldFilters` مع وصف ومثال (`contract_number:contains:A-12`)، وإلا يفشل `OpenApiDocumentationTests`؛ يُستعان بـ `add-endpoint-docs.mjs`.
- `Domain/Errors/DomainErrors.cs` (قسم `DocumentFields`): أكواد جديدة

| الكود | النوع | متى |
| --- | --- | --- |
| `DocumentFields.FilterRequiresDocumentType` | Validation | مرشح بلا `documentTypeId` |
| `DocumentFields.FilterMalformed` | Validation | غير `key:op:value` أو معامل غير معروف |
| `DocumentFields.FilterFieldNotFound` | Validation | مفتاح غير معرَّف لهذا النوع |
| `DocumentFields.FilterOperatorNotSupported` | Validation | معامل لا يناسب نوع الحقل |
| `DocumentFields.FilterValueInvalid` | Validation | قيمة لا تُحلَّل لنوع الحقل أو خيار غير موجود |
| `DocumentFields.TooManyFilters` | Validation | أكثر من 5 |

**الاختبارات** (`InstitutionalDiwan.Tests/Persistence`): كل نوع حقل ومعاملاته، AND بين مرشحين، وثيقة بلا قيمة لا تطابق، مرشح على وثيقة غير مرئية لا يتسرب، هروب `%` و`_`، تحليل القيمة التي تحوي `:`، وكل رمز خطأ. **خطر معروف:** مزوّد SQLite في الاختبارات لا يترجم مقارنات `decimal` بسهولة؛ إن تعذّر فتُجرى اختبارات الأرقام بتحويل مؤقت أو على SQL Server، ويُسجَّل ذلك في الوثيقة. هذه نقطة لم أتحقق منها بعد.

**القبول:** `dotnet test` أخضر، وتجربة حية: `GET /api/documents?documentTypeId=2&fieldFilters=...` بأنواع الحقول كلها، وكل رمز خطأ برسالة `ProblemDetails` صحيحة.

## 5. الواجهة

**البنية (FSD):**

| الطبقة | المحتوى |
| --- | --- |
| `entities/document` | نوع `DocumentFieldFilter { key, op, value }`، ودالتا `parseFieldFilters(searchParams)` و`serializeFieldFilters`، وإضافة `fieldFilters?: string[]` إلى معاملات `useDocuments` ومفتاح الاستعلام، وترميز المصفوفة المتكررة (`paramsSerializer: { indexes: null }` في هذا الطلب فقط حتى لا نغيّر باقي الطلبات) |
| `entities/document-schema` | موجود: `useDocumentTypeSchema` وأنواع الحقول. يُضاف جدول `OPERATORS_BY_DATA_TYPE` وتسمياتها العربية (يساوي، يحتوي، أكبر من، أكبر أو يساوي، أصغر من، أصغر أو يساوي) |
| `widgets/document-registry-table` | مكوّن فرعي `FieldFilterBar` (ضمن الودجت نفسه حفاظًا على سطر الجدول قصيرًا): زر «تصفية بالحقول» يفتح لوحة، وشارات للمرشحات الفعالة مع إزالة، وزر مسح |
| `shared/config` | دليل تعليمي `documents.field-filters`، و`ROUTES` بلا تغيير |
| `shared/api/errorMessages.ts` | الأكواد الستة بالعربية |

**السلوك:**
- المرشحات في الرابط كمعامل متكرر `f=contract_number:contains:A-12`. تغيير `documentTypeId` يمسحها لأن مجموعة الحقول تتغير.
- بدون نوع مختار: الزر معطّل مع تلميح «اختر نوع وثيقة أولًا». ونوع بلا مخطط منشور: «لا حقول لهذا النوع».
- كل صف في اللوحة: حقل (من المخطط المنشور بأسمائه العربية) ← معامل (حسب النوع) ← مُدخل مناسب: نص، رقم، تاريخ، قائمة نعم/لا، أو قائمة خيارات الحقل. أقصى 5 صفوف.
- التطبيق يعيد الصفحة إلى 1، ويتبع منطق `useTableState` في الرابط.
- حالة الخطأ: رسالة عربية عبر `toArabicErrorMessage` (قيمة غير صالحة تُمنع محليًا قبل الإرسال، والخادم هو الحكم النهائي).
- لا تغيير في الصلاحيات: المخطط المنشور يحتاج `DOCUMENT_VIEW` كالسجل نفسه.
- قائمة سياق الرأس تُضاف إليها «تصفية بالحقول» و«مسح مرشحات الحقول».
- RTL والمنطق: `ms-/me-`، وقيم الأرقام والتواريخ داخل `dir="ltr"`.

## 6. المراحل

| # | المرحلة | القبول |
| --- | --- | --- |
| ب1 | النطاق، الاستعلام، المستودع، الأخطاء | يبني ويمر الاختبار |
| ب2 | المتحكم + التوثيق OpenAPI + الاختبارات | `dotnet test` أخضر، `OpenApiDocumentationTests` يمر |
| ب3 | تجربة حية على API | كل الأنواع والأخطاء |
| 0 | أخطاء عربية + أنواع + ترميز المعاملات | `npm run build` ناجح |
| 1 | `FieldFilterBar` + ربط الرابط | تصفية فعلية على الجدول |
| 2 | أدلة تعليمية + قوائم السياق + الوثائق | `npm run lint` ناجح |
| 3 | تحقق حي بالمتصفح | قيمة لكل نوع حقل، مشاركة الرابط، تغيير النوع يمسح المرشحات، مستخدم بصلاحية محدودة |

**الوثائق:** README لكل مجلد مُعدّل، و`knowledge.md` (جملة «not implemented yet»، وقسم `GET /api/documents` ووصف `fieldFilters` والأكواد الجديدة)، وخطتا الحقول الديناميكية (السطر 17 و96 في خطة الواجهة)، وجدول الوحدات في README الجذر.

## 7. المخاطر ونقاط مفتوحة

| البند | المعالجة / التوصية |
| --- | --- |
| بطء `contains` على النص (لا فهرس) | محصور بنوع وثيقة وبـ `DocumentFieldId`، وحجم النص محدود. نقيس أولًا، ثم نقرر فهرسًا أو بحثًا نصيًا كاملًا |
| الحقول في نسخ المخطط القديمة | المخطط المنشور الحالي وحده يُعرض في اللوحة، فحقول نُسخ قديمة مسحوبة لا تُعرض. **التوصية:** قبول هذا القيد وتوثيقه |
| سجل الدائرة | هل يُكشف `fieldFilters` فيه؟ **التوصية:** لاحقًا، فالمستودع جاهز |
| البحث الحر داخل قيم الحقول النصية | خارج النطاق (تصفية، لا بحث) |
| أعمدة قيم الحقول في الجدول وتصديرها | خارج النطاق |
| مقارنة `decimal` في اختبارات SQLite | غير متحقق منها؛ انظر ب2 |
| ترميز `fieldFilters[]` في axios | يُعالج بـ `paramsSerializer` موضعي ويُجرَّب حيًا |
| تعدد المرشحات على الحقل نفسه (نطاق) | مسموح، وكل مرشح يُحسب مستقلًا |

## 8. خارج النطاق

OR بين المرشحات، ومرشحات متداخلة، وحفظ المرشحات كبحث محفوظ، وتصفية الأرشيف أو سجل الإحالات بالحقول، وإضافة صلاحية جديدة، وتغيير نموذج تخزين القيم.
