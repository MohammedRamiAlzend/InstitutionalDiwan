# الأشخاص المرتبطون بالوثائق (مدعى عليهم، مدعون، وغيرهم)

> **الحالة: منفّذة (2026-11-09).** `dotnet test` و`npm run build` و`npm run lint` ناجحة، وجُرّبت على API حي وSQL Server مؤقت ومن المتصفح. لم يُجرَّب: الجوال، ودخول مستخدم محدود النطاق من المتصفح (غُطّي باختبارات SQL)، وترقية قاعدة تحوي بيانات فعلية.
>
> **ما اختلف عن الخطة أثناء التنفيذ:** أسماء الجداول `PERSONS` و`DOCUMENT_PERSONS` و`DOCUMENT_PERSON_ROLES` لتطابق المشروع؛ أُضيف عمود `DisplayName` واسم عمود البحث `NormalizedName`؛ أشخاص الوثيقة يأتون من `GET /api/documents/{id}/persons` لا من `/details`؛ لا إرسال `RowVersion` من العميل (يبقى رمز تزامن داخل الحفظ)؛ حقل الاقتراحات داخل `entities/person` دون مكوّن `combobox` مشترك؛ حدث الإزالة يحفظ المعرّفات والصفة لا الاسم، لأن سجل الشخص باقٍ.

## الهدف

تسجيل أشخاص من خارج مستخدمي النظام على الوثيقة، لكل منهم صفة من جدول مرجعي (الافتراضية «مدعى عليه»). ومن اسم الشخص يصل المستخدم إلى وثائقه وحالتها ومكانها الحالي وكل ما جرى عليها.

## القرارات المعتمدة من المستخدم

| الموضوع | القرار |
| --- | --- |
| الهوية | سجل أشخاص موحّد `Persons` وجدول ربط `DocumentPersons`؛ لا سطر مكرر لكل وثيقة |
| الإلزام | **اختياري**: يمكن حفظ الوثيقة بدون أي اسم |
| نموذج الإنشاء | حقل اسم كامل فقط، مع إمكانية إضافة أكثر من شخص، والصفة تُسند تلقائيًا |
| الصلاحيات | `PERSON_VIEW` لشاشات الأشخاص و`PERSON_MANAGE` لتعديل بياناتهم، والوثائق الظاهرة مقيّدة بنطاق المستخدم |
| الاقتراحات | من كل الأشخاص، ببيانات مختصرة فقط، ولا تكشف وثائقهم |
| التعديل لاحقًا | إضافة وتغيير صفة وإزالة بصلاحية `DOCUMENT_EDIT` ما دامت الوثيقة غير مؤرشفة وغير ملغاة، مع تدقيق كل تغيير |
| بقية البيانات | عبر الـ API فقط الآن؛ الواجهة تعرض الاسم فقط |
| التتبع | شاشة «الأشخاص»، ملف الشخص مع وثائقه، وسجل زمني موحّد |
| حقول إضافية | أمانة/منطقة القيد، الجنس، الجنسية، الهاتف، العنوان، ملاحظات |

## قرارات التصميم

- **جدول منفصل عن `DocumentParticipant`.** المشارك الحالي مرسل/مستلم/نسخة/موقّع، ويشير إلى قسم أو جهة أو اسم حر. الطرف في القضية مفهوم آخر. لا ترحيل لصفوف `PersonName` القديمة ولا تغيير في تبويب «المشاركون» ولا في خيار «شخص» كمستلم.
- **جدول مرجعي جديد `DocumentPersonRole`** بدل توسيع `ParticipantRole`، حتى لا تختلط صفات القضية بأدوار المراسلة.
- **الصفة الافتراضية** تأتي من آلية `IsDefault` الموجودة، فيمكن تغييرها من شاشة الجداول المرجعية. إذا أُرسل الشخص بلا `roleId` يختار الخادم المدخل الافتراضي النشط، ثم `DefendantId` إن كان نشطًا، وإلا يرفض بـ `Persons.DefaultRoleMissing`.
- **كل الأعمدة تقبل `NULL`** كما طُلب. القيد الوحيد على مستوى النطاق: يُرفض شخص فارغ تمامًا (لا اسم كامل ولا اسم أول ولا كنية ولا رقم وطني) بـ `Persons.Empty`، لأن صفًا كهذا لا يمكن عرضه أو البحث عنه.
- **`FullName` عمود مستقل** لا يُقسَّم تلقائيًا إلى أول/أب/كنية؛ تقسيم الأسماء العربية آليًا غير موثوق. اسم العرض هو `FullName`، وإن كان فارغًا فتركيب الأول + الأب + الكنية.
- **الربط بشخص موجود يتم بالاختيار الصريح فقط.** الاسم المكتوب دون اختيار اقتراح ينشئ شخصًا جديدًا حتى لو طابق اسمًا موجودًا، لأن تطابق الاسم لا يثبت أنه الشخص نفسه.
- **الحفظ ذرّي:** الأشخاص يُرسلون داخل طلب إنشاء الوثيقة نفسه فيشملهم `TransactionPipelineBehavior`، بخلاف المستلم الحالي الذي يُضاف بطلب ثانٍ وقد يفشل وحده.
- **الإزالة حذف فعلي لسطر الربط** مع حدث تدقيق يحفظ الاسم والصفة. سجل الشخص نفسه لا يُحذف.
- **البحث العربي:** لا توجد Collation أو تطبيع في المشروع، فيُضاف عمود `NormalizedFullName` يُحسب في النطاق: إزالة التشكيل، توحيد `أ إ آ ← ا` و`ة ← ه` و`ى ← ي` و`ؤ ← و` و`ئ ← ي`، وضغط المسافات. يُطبَّع نص البحث بالطريقة نفسها وتُطابَق كل كلمة منه على حدة.
- **المكان الحالي للوثيقة:** الوثيقة لها حركة نشطة واحدة كحد أقصى (`Movements.ActiveMovementExists`)، فالمكان هو `ToDepartmentId` لخطوتها الحالية. بلا حركة نشطة: مكان الأرشفة إن كانت مؤرشفة، وإلا القسم المالك.

## نموذج البيانات

### `Persons` (جذر تجميع `Person` في `Domain/Entities/Persons/`)

| العمود | النوع | المعنى |
| --- | --- | --- |
| `PersonId` | `uniqueidentifier` PK | |
| `FullName` | `nvarchar(300)` | الاسم الكامل |
| `NormalizedFullName` | `nvarchar(300)` | للبحث، يكتبه النطاق |
| `FirstName`, `LastName`, `FatherName`, `MotherName` | `nvarchar(100)` | الاسم الأول، الكنية، اسم الأب، اسم الأم |
| `NationalNumber` | `nvarchar(20)` | نص حتى لا تضيع الأصفار البادئة |
| `CivilRegistryNumber` | `nvarchar(50)` | رقم القيد |
| `CivilRegistryPlace` | `nvarchar(200)` | أمانة/منطقة القيد |
| `BirthPlace` | `nvarchar(200)` | مكان التولد |
| `BirthDate` | `date` | تاريخ التولد |
| `GenderId`, `NationalityId` | `int` FK | جدولان مرجعيان `Gender` و`Nationality` |
| `Phone` | `nvarchar(30)` | |
| `Address` | `nvarchar(500)` | |
| `Notes` | `nvarchar(2000)` | |
| `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `RowVersion` | | أعمدة النظام، وهي الوحيدة غير القابلة لـ NULL مع المفتاح |

الفهارس: `NormalizedFullName`، وفهرس فريد مفلتر على `NationalNumber WHERE NationalNumber IS NOT NULL` (التعارض يعيد `Persons.NationalNumberTaken`).

### `DocumentPersons` (ابن في تجميع `Document`، مثل `DocumentParticipant`)

`DocumentPersonId` PK، `DocumentId` FK، `PersonId` FK، `RoleId` FK، `CreatedByUserId`، `CreatedAt`.

فهرس فريد `(DocumentId, PersonId)`: الشخص يظهر مرة واحدة بصفة واحدة في الوثيقة. فهرس `(PersonId, DocumentId)` لشاشة الشخص.

### الجداول المرجعية

| الرابط | الكيان | البذر |
| --- | --- | --- |
| `document-person-roles` | `DocumentPersonRole` | `DefendantId = 1` مدعى عليه (`IsDefault`)، `PlaintiffId = 2` مدعٍ |
| `genders` | `Gender` | ذكر، أنثى |
| `nationalities` | `Nationality` | فارغ، يُعبّأ من شاشة الجداول المرجعية |

## الـ API

| الطريقة والمسار | الصلاحية | الوظيفة |
| --- | --- | --- |
| `POST /api/documents`, `POST /api/documents/drafts` | كما هي | حقل جديد اختياري `persons: [{ personId?, fullName?, roleId? }]` |
| `GET /api/documents/{id}/details` | كما هي | يضيف `persons: [{ documentPersonId, personId, displayName, roleId }]`، بدون الرقم الوطني |
| `POST /api/documents/{id}/persons` | `DOCUMENT_EDIT` | إضافة شخص موجود أو جديد |
| `PUT /api/documents/{id}/persons/{documentPersonId}` | `DOCUMENT_EDIT` | تغيير الصفة |
| `DELETE /api/documents/{id}/persons/{documentPersonId}` | `DOCUMENT_EDIT` | إزالة الربط |
| `GET /api/persons/suggestions?q=` | `DOCUMENT_CREATE` أو `DOCUMENT_EDIT` | حتى 10 نتائج: `personId`, `displayName`, `birthYear`, `nationalNumberLast4`, `createdAt`. حرفان على الأقل، وبالاسم فقط |
| `GET /api/persons` | `PERSON_VIEW` | صفحة أشخاص: بحث بالاسم أو الرقم الوطني، فرز، `documentCount`, `lastActivityAt` |
| `GET /api/persons/{id}` | `PERSON_VIEW` | كل بيانات الشخص |
| `PUT /api/persons/{id}` | `PERSON_MANAGE` | تعديل البيانات، مع فحص `RowVersion` |
| `GET /api/persons/{id}/documents` | `PERSON_VIEW` | صفحة وثائق: العنوان، النوع، الرقم، الصفة، الحالة، المكان الحالي، آخر إجراء وتاريخه. فلاتر: الصفة، الحالة، بحث |
| `GET /api/persons/{id}/timeline` | `PERSON_VIEW` | صفحة أحداث موحّدة، الأحدث أولًا |

**قاعدة الوصول:** شاشات الأشخاص تمر كلها عبر `WhereViewableBy` الموجودة. الشخص يظهر فقط إذا ارتبط بوثيقة واحدة على الأقل يراها المستخدم، وإلا `404 Persons.NotFound` دون كشف وجوده، بما في ذلك `PUT`. العدد وآخر نشاط والسجل الزمني يُحسبون من الوثائق المرئية فقط.

**الاقتراحات** تتجاوز هذا الفلتر عمدًا لمنع التكرار بين الأقسام، لذا تعيد الحقول الخمسة أعلاه فقط. النتيجة الجانبية المقبولة: يمكن لمن ينشئ وثيقة أن يعرف أن اسمًا مسجل في النظام، لكن ليس أين ولا لماذا.

**السجل الزمني** يجمع في SQL عبر `Concat` مع فرز وترقيم على الخادم:

| `eventKind` | المصدر | ملاحظة |
| --- | --- | --- |
| `STATUS` | `DocumentStatusHistory` | يستثني الانتقال من/إلى «مؤرشف» لأن `ARCHIVE` يغطيه |
| `MOVEMENT` | `DocumentMovementEvent` مع الخطوة | من/إلى قسم، نوع الحدث، الملاحظات |
| `NUMBER` | `DocumentNumberHistory` | نوع الحدث فقط، كالخط الزمني الحالي |
| `ARCHIVE` | `DocumentArchiveEvent` | النوع والمكان |
| `PERSON` | `DocumentPersons` | ربط هذا الشخص بالوثيقة وصفته |

كل صف يحمل `documentId` و`documentTitle` و`occurredAt` و`actorUserId`. تبني الواجهة النص العربي من الحقول البنيوية كما يفعل `DocumentTimeline.tsx`.

## مراحل التنفيذ

### 1. النطاق (`InstitutionalDiwan.Domain`)

- [x] `Entities/Persons/Person.cs`: `Create`, `UpdateDetails`, وحساب `NormalizedFullName` واسم العرض، و`ValueObjects/PersonName` أو مساعد التطبيع.
- [x] `Entities/Persons/PersonsLookups.cs`: `DocumentPersonRole`, `Gender`, `Nationality`.
- [x] `Entities/Correspondence/DocumentPerson.cs`.
- [x] `Document.cs`: `_persons` مع `LinkPerson`, `ChangePersonRole`, `UnlinkPerson`. ترفض المؤرشفة بـ `Correspondence.DocumentArchived` والملغاة بـ `Persons.DocumentCanceled`. (تنبيه: `AddParticipant` الحالي لا يرفض الملغاة في النطاق، فلا يُنسخ منه كما هو.)
- [x] `Errors/DomainErrors.cs`: مجموعة `Persons`.
- [x] `Events/DomainEvents.cs`: `DocumentPersonLinkedEvent`, `DocumentPersonRoleChangedEvent`, `DocumentPersonUnlinkedEvent`, `PersonDetailsUpdatedEvent`.
- [x] `Common/Permissions.cs`: `PERSON_VIEW`, `PERSON_MANAGE` (20 رمزًا).
- [x] `Repositories/IDomainRepositories.cs`: `IPersonRepository`.
- [x] `AuditLookups.cs`: `AuditEntityType.PersonId = 15`.

### 2. البنية التحتية (`InstitutionalDiwan.Infrastructure`)

- [x] تكوينات EF للجداول وDbSets في `DiwanDbContext.cs`، وترحيل واحد `Add_Persons`.
- [x] `DocumentRepository.GetByIdAsync`: إضافة `Include(document => document.Persons)` (موجود ضمن `AsSplitQuery`).
- [x] `PersonRepository`: القوائم والاقتراحات والوثائق والسجل الزمني، باستخدام `WhereViewableBy`.
- [x] `LookupRepository.LookupCatalog`: الروابط الثلاثة.
- [x] البذر: ثلاثة Seeders على نمط `LookupSeederBase` مع `MarkDefault` للمدعى عليه كما في `DocumentTypeSeeder`، و`PermissionSeeder`، ومنح الصلاحيتين لـ `ADMIN` في `RoleSeeder`، وصف `AuditEntityType` الجديد.
- [x] `TamperEvidentAuditInterceptor`: ربط الأحداث الجديدة. **يُسجَّل في التدقيق أسماء الحقول المتغيرة، لا قيم الرقم الوطني والهاتف والعنوان.**

### 3. التطبيق والخادم

- [x] `DocumentPersonInput` ومساعد مشترك `DocumentPersonLinker` يُستخدم في `CreateDocumentCommandHandler` و`CreateDraftDocumentCommandHandler` وأمر الإضافة: يتحقق من الصفة النشطة، يحل الافتراضية، ينشئ الشخص أو يجلبه، يمنع التكرار داخل الوثيقة بـ `Persons.DuplicateInDocument`.
- [x] المحققات: إما `personId` أو `fullName`، حد 50 شخصًا في الطلب، حدود أطوال الأعمدة.
- [x] `Features/Persons/`: أوامر واستعلامات الجدول أعلاه.
- [x] `Features/Correspondence/Documents/`: أوامر الإضافة وتغيير الصفة والإزالة، بنفس فحص الوصول في `AddDocumentParticipantCommandHandler`.
- [x] إضافة `Persons` إلى `CreateDocumentRequest`، وطلب المسودة، واستجابة التفاصيل.
- [x] `PersonsController`، ومسارات الوثيقة في `DocumentsController`، مع ملخص ووصف ومثال لكل معامل (يفرضها `OpenApiDocumentationTests`).
- [x] `Extensions/AuthenticationExtensions.cs`: سياسة مركّبة للاقتراحات (أيٌّ من `DOCUMENT_CREATE` و`DOCUMENT_EDIT`)، لأن كل سياسة حالية تقابل صلاحية واحدة.

### 4. الواجهة: الإعدادات المشتركة

- [x] `shared/config/permissions.ts` (20 رمزًا)، `routes.ts` (`PERSONS`, `PERSON_DETAILS`)، `lookupCatalogs.ts`، `lookupIds.ts`.
- [x] `entities/lookup/model/constants.ts`: الروابط والعناوين الاحتياطية.
- [x] `shared/api/errorMessages.ts`: رسائل عربية لرموز `Persons.*`.
- [x] `shared/ui/combobox.tsx`: لا يوجد مكوّن إكمال تلقائي حاليًا. مبني على Radix Popover، قابل للوحة المفاتيح، RTL.

### 5. الواجهة: إنشاء الوثيقة

- [x] `entities/person`: `api/personApi.ts`, `model/types.ts`, `model/queries.ts` (مصنع `personKeys`)، `index.ts`.
- [x] `entities/person/ui/PersonNamesInput.tsx`: حقل واحد مع اقتراحات، ورقائق للأسماء المضافة مع زر إزالة. Enter أو خيار «إضافة "الاسم" كشخص جديد» يضيف جديدًا. مكانه `entities` لأن `features/document-create` لا يستورد من شريحة مجاورة، كما في `DynamicFieldInputs`.
- [x] `features/document-create`: حقل `persons` في `createDocumentSchema` و`buildEmptyValues` و`shared`، وإدراجه في `hasDraftContent` حتى تعمل الاستعادة التلقائية للمسودة. نص مساعد يعرض الصفة الافتراضية من الجدول المرجعي.
- [x] تكرار الاسم المطبَّع في القائمة يمنعه الحقل برسالة عربية.

### 6. الواجهة: تبويب الوثيقة

- [x] `features/document-person-manage`: طفرات الإضافة وتغيير الصفة والإزالة، تُبطل مفاتيح تفاصيل الوثيقة والشخص، مع حوارات عربية.
- [x] `pages/documents/ui/DocumentPersonsTab.tsx`: جدول `document-persons` كامل العقد (بحث، فرز، أعمدة محفوظة، قائمة سياق مفلترة بالصلاحيات). الاسم رابط إلى ملف الشخص لمن يملك `PERSON_VIEW`.
- [x] `DocumentDetailsPage.tsx`: تبويب `persons` بعنوان «الأشخاص المرتبطون»، وتجميد التعديل للمؤرشفة والملغاة مع ذكر السبب.

### 7. الواجهة: شاشات الأشخاص

- [x] `widgets/persons-table` (`diwan.table.persons.columns.v1`): الاسم، عدد الوثائق، آخر نشاط.
- [x] `widgets/person-documents-table` (`diwan.table.person-documents.columns.v1`): العنوان، النوع، الرقم، الصفة، الحالة، المكان الحالي، آخر إجراء، مع رابط للوثيقة.
- [x] `widgets/person-timeline`: السجل الموحّد مرقّم، كل حدث يذكر وثيقته برابط.
- [x] `pages/persons`: `PersonsPage` و`PersonDetailsPage` (تبويبا «الوثائق» و«السجل الزمني» في `?tab=`). الفلاتر والبحث والصفحة في الرابط.
- [x] `shell-nav.ts`: «الأشخاص» في مجموعة «المراسلات» بصلاحية `PersonView`، و`AppRouter.tsx`.
- [x] `shared/config/guides/persons.ts` وتسجيله، ومعرفات تبويب الوثيقة في `guides/documents.ts`.

### 8. التوثيق والاختبارات

**الاختبارات الخلفية:**

- [x] النطاق: `Person` (الفراغ، التطبيع)، `Document.LinkPerson` (مكرر، مؤرشفة، ملغاة).
- [x] المعالجات: إنشاء بأشخاص جدد وموجودين، الصفة الافتراضية وتغييرها من الجدول، الفشل يلغي كل شيء.
- [x] استمرارية SQL على نمط `DocumentViewAccessQueryTests`: لا يظهر الشخص أو وثائقه أو أحداثها خارج النطاق، والوثيقة السرية لا تدخل في العدد ولا السجل؛ الاقتراحات لا تكشف غير حقولها؛ فهرس الرقم الوطني يقبل عدة `NULL`.
- [x] تحديث `LookupHandlerTests` (قائمة الروابط)، و`SeedingPipelineTests`، و`PermissionSeederTests`، و`RoleSeederTests`، و`FakeDocumentRepository`.

**التوثيق:** README كل مجلد تغيّر (الجدول، الحالة، سطر Change Log)، و`knowledge.md`، وعدد الصلاحيات في `CLAUDE.md` من 18 إلى 20.

**معيار الإنجاز:**

```bash
dotnet test InstitutionalDiwan.slnx
```

```bash
npm run build
```

```bash
npm run lint
```

ثم تجربة حية: إنشاء وثيقة بثلاثة أسماء (واحد مقترح، اثنان جديدان) بمساري المسودة والتسجيل، إرسالها حركة، فتح ملف الشخص والتأكد من الوثيقة ومكانها وأحداثها، الدخول بمستخدم من قسم آخر والتأكد من الحجب، تغيير الصفة الافتراضية من الجداول المرجعية، والوضعين الفاتح والداكن.

## المخاطر

- **التكرار:** الربط يعتمد على اختيار المستخدم، والمقترحات لن تحمل سنة ولادة أو رقمًا وطنيًا حتى تُعبّأ عبر الـ API. لذلك يظهر تاريخ إضافة الشخص كمميّز ثالث، ويبقى دمج المكررين عملًا لاحقًا.
- **الأداء:** البحث `Contains` على `NormalizedFullName` لا يستفيد من الفهرس. مقبول للأحجام المتوقعة؛ وعند الحاجة يُستبدل بـ Full-Text Search دون تغيير العقد.
- **`WhereViewableBy` داخل تجميع الأشخاص** استعلام ثقيل؛ يُفحص ناتج SQL على بيانات بذر كبيرة قبل الإغلاق.
- **بيانات شخصية:** الرقم الوطني والهاتف محجوبان عمن لا يملك `PERSON_VIEW`، لكن قراءتها غير مسجلة في التدقيق في هذه المرحلة.

## خارج النطاق

نموذج تعديل بقية بيانات الشخص في الواجهة، دمج الأشخاص المكررين، فلتر باسم الشخص في سجل الوثائق والأرشيف، إضافة الحركات إلى الخط الزمني لكل وثيقة، ترحيل `DocumentParticipant.PersonName`، وتدقيق عمليات القراءة.

## إضافة لاحقة: إلغاء عنوان الوثيقة وملخصها (2026-11-09)

| الموضوع | القرار |
| --- | --- |
| النطاق | لا يُطلبان ولا يُعرضان في الواجهة. العمودان يقبلان `NULL` وتبقى قيم الوثائق القديمة محفوظة |
| اسم الوثيقة | النوع + رقم سجل القسم المالك، والمسودة النوع + تاريخ الإنشاء |

- `Document.DisplayName` عمود يكتبه النظام، ويحل محل العنوان في الجداول والإشعارات والبحث والفرز. حقل `title` في الاستجابات يحمل هذا الاسم فلم تتغير عقود القراءة.
- يأخذ الاسم رقمه من أول سطر للوثيقة في سجل قسمها المالك، وأسطر السنوات اللاحقة والأقسام الأخرى لا تغيّره.
- حُذف إجراء «تعديل المسودة» لأنه لم يكن يعدّل غير العنوان والملخص. `PUT /api/documents/{id}` باقٍ دون استخدام من الواجهة.
- البحث لم يعد يقرأ العناوين والملخصات القديمة المخفية.
- تغيير تسمية نوع الوثيقة لاحقًا لا يحدّث أسماء الوثائق المكتوبة سابقًا.

## إضافة لاحقة: حذف تاريخ الوثيقة والمرجع الخارجي (2026-11-09)

حُذفت الأعمدة `DocumentDate` و`ExternalReferenceNumber` و`ExternalDocumentDate` من `DOCUMENTS` نهائيًا، بالترحيل `Remove_Document_Date_And_External_Reference`، لأن المشروع لم يصل إلى الإنتاج. ما يلزم منها يُعرَّف كحقل ديناميكي. سنة الترقيم الاحتياطية أصبحت سنة إنشاء الوثيقة، وهي نفس القيمة السابقة حين لا يُدخل تاريخ.
