# الخطة — المودول 07: الأرشفة (الباك إند)

**الحالة:** منفّذة (2026-11-03): المراحل 0 إلى 5 منفّذة ومختبرة (المجموع 637 اختبارًا) ومتحقق منها على الـ API الحي بقاعدة مؤقتة. الـ migration لم يُطبَّق على قاعدة التطوير بعد، ويُطبَّق عند أول تشغيل. الباقي: خطة الواجهة، والثغرة المذكورة في §7.
**النطاق:** `InstitutionalDiwan.Domain` / `Application` / `Infrastructure` / `Server` / `Tests`. واجهة الأرشفة في خطة منفصلة لاحقًا.
**آخر مراجعة:** 2026-11-03.

---

## 1. الوضع الحالي (بعد مراجعة الكود)

| الموجود | غير الموجود |
| --- | --- |
| `Document.Archive` / `Unarchive` مع `StatusBeforeArchiveId` وأحداث الدومين | لا أحد يستدعيهما، ولا اختبار يغطيهما |
| كيانا `DocumentArchiveEvent` و`SavedSearch` | لا معالجات Application ولا مستودعات لهما |
| جدولا `DOCUMENT_ARCHIVE_EVENTS` و`SAVED_SEARCHES` في الـ migrations | لا Controllers ولا Contracts |
| lookups `archive-event-types` و`archive-locations` مع seed | لا صلاحية خاصة بالأرشفة |
| تسجيل حدثي الأرشفة وإلغائها في سلسلة التدقيق | لا FK على `DocumentId` و`ActorUserId` و`SavedSearch.UserId` |

بما أن `Archive` بلا مستدعين، فلا توجد وثائق مؤرشفة بلا `DocumentArchiveEvent`، ولا حاجة لترحيل بيانات (backfill).

### ثغرات يجب أن تعالجها هذه الخطة

1. **الوثيقة المؤرشفة ليست مجمّدة.** لا يوجد فحص لحالة الوثيقة في: `AddDocumentParticipant` (يفحص الإلغاء فقط)، `CancelOfficialNumber`، `ReplaceOfficialNumber`، `RecordDepartmentRegistration`، `CreateDocumentFile`، `CreateDocumentFileWithVersion`، `UploadDocumentFileVersion`، `ArchiveDocumentFile`.
2. **`Document.Archive` يقبل المسودات**، فيمكن أرشفة وثيقة بلا رقم رسمي.
3. **تدقيق إلغاء الأرشفة خاطئ:** في `TamperEvidentAuditInterceptor` توضع الحالة المستعادة في `OldValuesJson`، والصحيح أن القديمة `ARCHIVED` والجديدة هي المستعادة.
4. **`AuditActionMiddleware`:** البادئة `Archive` لا تطابق `UnarchiveDocument`، فلن يُصنَّف الإجراء `Update`.
5. **التعارض المتزامن:** `Document.RowVersion` مفعّل، لكن لا توجد أي معالجة لـ `DbUpdateConcurrencyException`. سباق الأرشفة مع إنشاء حركة يجب أن يعود 409 لا 500.
6. **حدود الطول:** `SavedSearch` لا يفحص طول `Name` (العمود 150)، و`DocumentArchiveEvent` لا يفحص طول `Reason` (العمود 500).

## 2. القرارات المطلوب اعتمادها (المقترح أولًا)

| # | القرار | المقترح | السبب |
| --- | --- | --- | --- |
| 1 | صلاحية الأرشفة | صلاحية جديدة `DOCUMENT_ARCHIVE` (يصبح العدد 17)، تُمنح لـ ADMIN فقط في الـ seed | `DOCUMENT_EDIT` يملكها دور USER، فإعادة استخدامها تتيح الأرشفة لكل مستخدم؛ و`DOCUMENT_APPROVE` معناها اعتماد الصادر. **الأثر:** مرآة في `permissions.ts` وتحديث 8 assertions في الاختبارات |
| 2 | ما الذي يمكن أرشفته | `REGISTERED` فقط | المسودة بلا رقم رسمي، و`IN_MOVEMENT` مرفوضة أصلًا لوجود حركة نشطة |
| 3 | تجميد الوثيقة المؤرشفة | كل التعديلات مرفوضة بـ 409 `Correspondence.DocumentArchived`؛ القراءة والتنزيل والتحقق من البصمة ومنح الوصول تبقى متاحة | السجل المؤرشف لا يتغير إلا بعد إلغاء أرشفته صراحةً |
| 4 | موقع الأرشيف | إجباري عند الأرشفة ومن lookup نشط | بلا موقع يفقد الكيان والبحث قيمتهما |
| 5 | السبب | اختياري عند الأرشفة، **إجباري** عند إلغائها | إلغاء الأرشفة استثناء يجب تبريره، كما في الإلغاء |
| 6 | مدد الحفظ (Retention) | **خارج النطاق** | lookup `RetentionAppliedId` موجود فقط، ولا يوجد نموذج لمدة الحفظ ولا قواعد للإتلاف؛ تحتاج متطلبات مستقلة |
| 7 | البحث المحفوظ | شخصي لكل مستخدم بلا مشاركة، وخاص ببحث الأرشيف | هذا ما يدعمه الكيان الحالي |

## 3. المسارات

| المسار | الصلاحية | فحص الوصول |
| --- | --- | --- |
| `POST /api/documents/{id}/archive` | `DOCUMENT_ARCHIVE` | `CanAccessDocumentAsync(MANAGE)` |
| `POST /api/documents/{id}/unarchive` | `DOCUMENT_ARCHIVE` | `CanAccessDocumentAsync(MANAGE)` |
| `GET /api/documents/{id}/archive-events` | `DOCUMENT_VIEW` | `CanAccessDocumentAsync(VIEW)` |
| `GET /api/archive/documents` | `DOCUMENT_VIEW` | تصفية على مستوى SQL (§4.2) |
| `GET` / `POST /api/saved-searches` | `DOCUMENT_VIEW` | مالك البحث فقط |
| `PUT` / `DELETE /api/saved-searches/{id}` | `DOCUMENT_VIEW` | مالك البحث فقط؛ بحث مستخدم آخر يعود 404 |

ملاحظة: `MANAGE` عبر النطاق وحده يتطلب نطاقًا مؤسسيًا (قاعدة `DocumentAccessEvaluator` الحالية)؛ فموظف الأرشيف بنطاق قسم يحتاج منحة `MANAGE` على الوثيقة أو نطاقًا مؤسسيًا. هذا مقصود ومطابق لسلوك المرفقات.

## 4. القواعد

### 4.1 الأرشفة وإلغاؤها
- العملية ذرية داخل `TransactionPipelineBehavior`: تغيير حالة الوثيقة + `DocumentStatusHistory` + `DocumentArchiveEvent` + صف التدقيق.
- ترتيب الفحص: الوجود ← مستوى الوصول ← موقع الأرشيف ← الحركات النشطة ← قواعد الدومين.
- يضاف `ArchiveLocationId` إلى `DocumentArchivedEvent` ليظهر الموقع في سلسلة التدقيق.
- `Timeline` يبقى كما هو (يقرأ `StatusHistory`)، والتفاصيل في `archive-events`.

### 4.2 بحث الأرشيف
- الفلاتر: `search`, `owningDepartmentId`, `documentTypeId`, `directionId`, `archiveLocationId`, `archivedFrom`, `archivedTo`؛ الترتيب الافتراضي `archivedAt` تنازليًا؛ الحجم من `PageRequest`.
- الصف يحمل حقول الوثيقة مع `archivedAt` و`archivedByUserId` و`archiveLocationId` من آخر حدث `ArchivedId`.
- **لا يُفحص كل صف بعد جلب الصفحة** لأن ذلك يكسر العدّ والترقيم. تُطبَّق قاعدة `VIEW` في الاستعلام نفسه:
  `CreatedByUserId = me` أو منحة سارية للمستخدم أو (القسم ضمن النطاق و السرية ليست `SECRET`/`TOP_SECRET`).
- يتطلب ذلك إضافة `GetAccessibleDepartmentIdsAsync` إلى `IDepartmentScopeEvaluator` (تعيد `null` للنطاق المؤسسي)، مع إعادة استخدام المنطق الداخلي نفسه كي لا ينحرف الفحصان.
- مسار مشاركة الحركة لا ينطبق: الوثيقة المؤرشفة لا تملك حركة نشطة.

### 4.3 البحث المحفوظ
- `CriteriaJson` يُفكّ إلى `ArchiveSearchCriteria` المكتوب ويُرفض غير الصالح (`Archiving.CriteriaInvalid`) مع حد أقصى 4000 حرف. **الخادم لا ينفذ المعايير**: الواجهة تقرؤها وتستدعي البحث بها، فيبقى فحص الوصول هناك.
- `Name` فريد لكل مستخدم (`Archiving.SearchNameTaken`) وبحد 150 حرفًا؛ وحد أقصى 50 بحثًا للمستخدم (`Archiving.SearchLimitReached`).
- بحث افتراضي واحد لكل مستخدم: المعالج يزيل الافتراضي السابق، وفهرس فريد مفلتر يحمي من السباق.
- الحذف فعلي؛ فهو تفضيل شخصي لا سجل مؤسسي، ولذلك لا يُضاف `SavedSearch` إلى خريطة التدقيق بالكيانات (يبقى مسجلًا عبر طبقة تدقيق الإجراءات).

## 5. المراحل

### المرحلة 0 — Domain (منفّذة)
- **تعديل عن الخطة الأصلية:** الصلاحية نُفّذت كاملة هنا (`PermissionSeeder` و`RoleSeeder` وassertions الاختبارات ومرآة الواجهة) لأن إضافتها إلى `Permissions.All` وحدها تكسر الاختبارات وتخالف قاعدة تطابق الصلاحيات.
- `Permissions.DocumentArchive` وإضافتها إلى `All`.
- `Document.Archive`: `REGISTERED` فقط، خطأ جديد `Correspondence.OnlyRegisteredArchivable`.
- `Document.EnsureNotArchived()` وخطأ `Correspondence.DocumentArchived`، واستخدامه داخل `AddParticipant` و`AttachReplacementNumber`.
- `SavedSearch`: حد طول الاسم و`ClearDefault()`. `DocumentArchiveEvent.Record`: حد طول السبب.
- أخطاء `Archiving.*`: `LocationNotFound`, `UnarchiveReasonEmpty`, `ReasonTooLong`, `SearchNotFound`, `SearchNameTooLong`, `SearchNameTaken`, `SearchLimitReached`, `CriteriaInvalid`.
- عقدا `IDocumentArchiveRepository` (إضافة الحدث، سجل الوثيقة، البحث المرقّم مع `ArchiveSearchFilter` و`ArchivedDocumentRow`) و`ISavedSearchRepository`.
- توسيع `DocumentArchivedEvent` بـ `ArchiveLocationId`.

### المرحلة 1 — Infrastructure (منفّذة)
- تنفيذ المستودعين وتسجيلهما، و`GetAccessibleDepartmentIdsAsync`.
- تصحيح تدقيق إلغاء الأرشفة وإضافة الموقع لتدقيق الأرشفة.
- Migration إضافي `Add_Archiving_Constraints`: FK لـ `DocumentId` و`ActorUserId` و`PreviousStatusId`/`NewStatusId` و`SavedSearch.UserId`؛ فهرس فريد `(UserId, Name)`؛ فهرس فريد مفلتر `(UserId) WHERE IsDefault = 1`؛ فهرس `(EventTypeId, OccurredAt)` لبحث الأرشيف.
- تم التأكد أن `Add_Document_Dynamic_Fields` مطبّق على قاعدة التطوير.

### المرحلة 2 — Application (منفّذة)
- `Features/Archiving/`: `ArchiveDocumentCommand`, `UnarchiveDocumentCommand`, `ListDocumentArchiveEventsQuery`, `SearchArchivedDocumentsQuery` مع validators والـ responses.
- `Features/Archiving/SavedSearches/`: `Create`, `Update`, `Delete`, `List`.
- **التجميد:** فحص `DocumentArchived` في المعالجات الواردة في §1 (البند 1) التي لا يغطيها الدومين، مع إبقاء القراءة متاحة.

### المرحلة 3 — Server (منفّذة)
- `DocumentArchiveController` و`SavedSearchesController` و`Contracts/Archiving/`.
- ملخص ووصف ومثال لكل مسار ومعامل (`OpenApiDocumentationTests` يفشل بدونها؛ `add-endpoint-docs.mjs` يساعد).
- إضافة `Unarchive` إلى قائمة بادئات `AuditActionMiddleware`.
- تحويل `DbUpdateConcurrencyException` إلى 409 ProblemDetails بكود `Common.ConcurrencyConflict`.

### المرحلة 4 — Tests (منفّذة)
- **Domain:** جدول انتقالات `Archive`/`Unarchive` بكل الحالات، استعادة `StatusBeforeArchiveId`، قواعد `SavedSearch`.
- **Application:** الفحوص الأربعة لكل أمر، وحدث الأرشفة يُكتب في العملية نفسها، وعدم تسرب بحث مستخدم آخر.
- **حالات حرجة:**
  - كل معالج من معالجات التجميد الثمانية يرفض الوثيقة المؤرشفة، ويعود للعمل بعد إلغاء الأرشفة.
  - **تطابق الفحصين:** كل صف يعيده بحث الأرشيف يجتاز `CanAccessDocumentAsync(VIEW)`، والعكس، لكل نوع نطاق وكل مستوى سرية ومنحة منتهية.
  - وثيقة `SECRET` لا تظهر بالنطاق وحده ولا تدخل في `TotalCount`.
  - سباق الأرشفة مع إنشاء حركة يعود 409.
  - فهرس البحث الافتراضي يرفض افتراضيًا ثانيًا.
- **Persistence:** الفهارس الفريدة وFK على SQL Server، وصحة ترجمة استعلام البحث.
- **Server:** الصلاحيات (403)، وOpenAPI، وتصنيف التدقيق.

### المرحلة 5 — التوثيق والمرآة (منفّذة)
- `README.md` لكل مجلد تغيّر (Contents + Implementation status + Change Log)، وREADME جديد لـ `Features/Archiving/` و`SavedSearches/` و`Contracts/Archiving/`، وصف المودول 07 في `README.md` الجذر.
- `knowledge.md`: المسارات الجديدة بوسم «باك إند فقط، غير مطبّق في الواجهة»، وإزالة 07 من قائمة غير المنفّذ.
- **التعديل الوحيد خارج الباك إند:** إضافة الكود إلى `src/shared/config/permissions.ts`، وتحديث ذكر «16» في `CLAUDE.md` و`README.md` و`AGENTS.md`، لأن القاعدة توجب تطابق الصلاحيات.

### لاحقًا — خطة الواجهة (منفصلة)
شاشة الأرشيف، إجراءات الأرشفة في تفاصيل الوثيقة، البحث المحفوظ، ترجمة الأخطاء الجديدة إلى العربية، وأدلة الوضع التعليمي. حتى ذلك الحين يرى المستخدم رسالة الخطأ العامة إذا وصل إلى وثيقة مؤرشفة عبر الـ API، وهو غير ممكن من الواجهة لأنها لا تملك إجراء الأرشفة.

## 6. التحقق

`dotnet build InstitutionalDiwan.slnx` و`dotnet test InstitutionalDiwan.slnx` نظيفان؛ ثم على الـ API الحي (Scalar): وثيقة مسجلة ← أرشفة ← رفض رفع مرفق وإضافة مشارك واستبدال الرقم (409) ← ظهورها في البحث ← مراجعة `/api/audit-logs` ← إلغاء الأرشفة بسبب ← عودة التعديل. وتكرار البحث بحساب `user` للتأكد من الإخفاء.

**نتيجة التحقق (2026-11-03):** كل خطوات السيناريو أعلاه نجحت على قاعدة مؤقتة، إضافةً إلى طلبات متوازية: 8 طلبات أرشفة لنفس الوثيقة في 3 جولات أعطت نجاحًا واحدًا وحدثًا واحدًا والباقي 409 دون أي 500، و8 طلبات إنشاء بحث افتراضي بنفس الاسم أبقت افتراضيًا واحدًا. سلسلة التدقيق بقيت سليمة.

## 7. خارج النطاق

- **مدد الحفظ والإتلاف** (القرار 6).
- **الأرشفة الجماعية** ونقل الوثيقة بين مواقع الأرشيف.
- **مرفقات الوثيقة المؤرشفة:** لا تُؤرشف تلقائيًا؛ التجميد يكفي، و`DocumentFile.IsArchived` يبقى مفهومًا مستقلًا.
- **ثغرة قائمة تتجاوز هذا المودول:** معالجات `Features/Correspondence/Documents` لا تستدعي `IDocumentAccessEvaluator`، و`GET /api/documents` غير مقيّد بالنطاق ولا بالسرية. يعني ذلك أن `?statusId=4` يعرض الوثائق المؤرشفة لكل حامل `DOCUMENT_VIEW` متجاوزًا تصفية §4.2. تُعالج في مهمة مستقلة لأنها تغيّر سلوك مسارات قائمة.
