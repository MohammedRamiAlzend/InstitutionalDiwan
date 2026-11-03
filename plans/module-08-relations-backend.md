# الخطة — المودول 08: الروابط بين الوثائق (الباك إند)

**الحالة:** منفّذة (2026-11-03): المراحل 0 إلى 5 منفّذة ومختبرة (المجموع 718 اختبارًا). لم يُتحقق منها بعد على الـ API الحي، والـ migration لم يُطبَّق على قاعدة التطوير بعد. حُسمت القرارات الثمانية في §2 كما اقتُرحت، وحُسم اصطلاح الحذف: السبب في جسم الطلب (JSON). قفل الدورات `sp_getapplock` لا يعمل إلا على SQL Server فلا تغطيه الاختبارات (SQLite).
**النطاق:** `InstitutionalDiwan.Domain` / `Application` / `Infrastructure` / `Server` / `Tests`. واجهة الروابط في خطة منفصلة لاحقًا.
**آخر مراجعة:** 2026-11-03.

---

## 1. الوضع الحالي (بعد مراجعة الكود)

| الموجود | غير الموجود |
| --- | --- |
| `DocumentRelation` (جذر تجميع) مع `Create` يطبّق: منع الربط الذاتي، رفض نوع غير فعّال، منع الدورة، توحيد طرفي الرابط المتماثل | لا توجد `Remove` على الجذر، فلا يُرفع `DocumentRelationRemovedEvent` أبدًا |
| `DocumentRelationType` مع `IsSymmetric` و`PreventCycles` و`NormalizeEndpoints`؛ والـ seed: ردّ على، مرتبط (متماثل بلا منع دورات)، يلغي، نسخة عن | لا يوجد مستودع، ولا تسجيل في DI (`IDocumentRelationRepository` عقد فقط) |
| `DocumentRelationEvent.Record` و`RelationActionType` (Created/Updated/Removed) | لا أحد يكتب أحداث الروابط |
| جداول `DOCUMENT_RELATIONS` و`DOCUMENT_RELATION_EVENTS` وفهرس فريد `(Source, Target, Type)` | لا FK على `SourceDocumentId` و`TargetDocumentId` و`CreatedByUserId` و`ActorUserId` |
| أخطاء `Relations.*` الخمسة وحدثا الدومين، وربطهما في `TamperEvidentAuditInterceptor` | لا Handlers ولا Controllers ولا Contracts، ولا فحص وصول |

لا يوجد كاتب لهذه الجداول، فلا حاجة لترحيل بيانات (backfill).

### ثغرات يجب أن تعالجها هذه الخطة

1. **كشف الدورات غير منفَّذ:** `DocumentRelation.Create` يستلم `wouldIntroduceCycle` جاهزًا ولا شيء يحسبه. المطلوب استعلام وصول (reachability) على الرسم البياني لنوع الرابط نفسه.
2. **سباق الدورات:** طلبان متوازيان `A→B` و`B→A` يمرّ كلٌّ منهما فحص الدورة ثم يُحفظان. الفهرس الفريد لا يمنعه لأن الصفّين مختلفان. يلزم قفل على مستوى النوع داخل المعاملة.
3. **حذف الرابط:** لا طريقة في الدومين تُنتج حدث الإزالة. وبما أن الحذف فعلي (الفهرس الفريد يمنع إعادة الإنشاء مع الحذف الناعم)، يجب أن يبقى `DocumentRelationEvent` هو السجل الدائم، وبالتالي **لا يجوز وضع FK من `DOCUMENT_RELATION_EVENTS.RelationId` إلى `DOCUMENT_RELATIONS`**.
4. **لا فحص وصول:** الربط يكشف عنوان الوثيقة الأخرى ورقمها، فيجب أن يكون مقيَّدًا بصلاحية الاطلاع على الطرفين.
5. **الوثيقة المؤرشفة:** قرار المودول 07 يجمّد كل كتابة على الوثيقة المؤرشفة؛ والربط بوثيقة مؤرشفة حالة شائعة (رد جديد على كتاب مؤرشف) فتحتاج قاعدة صريحة (§2 قرار 3).
6. **تدقيق الطرف الثاني:** `RelatedDocumentId` في صف التدقيق هو `SourceDocumentId` فقط، فالبحث في التدقيق بوثيقة الهدف لا يجد الرابط. الـ JSON يحمل المعرفين. يُوثَّق كقيد معروف ولا يُغيَّر الآن.
7. **حدود الطول:** `Notes` (500) و`Reason` (500) بلا فحص في الدومين.
8. **حجم الرسم البياني:** لا حد على العمق ولا على عدد العقد.

## 2. القرارات المطلوب اعتمادها (المقترح أولًا)

| # | القرار | المقترح | السبب |
| --- | --- | --- | --- |
| 1 | الصلاحية | إعادة استخدام `DOCUMENT_EDIT` للإنشاء والحذف، و`DOCUMENT_VIEW` للقراءة. **لا صلاحية جديدة** (يبقى العدد 17) | الربط عمل كتابي روتيني لا حساس كالأرشفة؛ وتجنّب تغيير الـ seed ومرآة `permissions.ts` و8 assertions |
| 2 | فحص الوصول | إنشاء: `DOWNLOAD` على وثيقة المسار و`VIEW` على الوثيقة الأخرى. حذف: `DOWNLOAD` على وثيقة المسار. قراءة: `VIEW` | يطابق قاعدة تعديل المسودة في §7 من خطة 07؛ ولا يُكشف شيء عن وثيقة لا يراها المستخدم |
| 3 | الوثيقة المؤرشفة | الرفض بـ 409 `Correspondence.DocumentArchived` إذا كانت **وثيقة المسار** مؤرشفة؛ والطرف الآخر يجوز أن يكون مؤرشفًا | وثيقة المسار هي ما يُعدَّل سجلها؛ ويبقى «رد على كتاب مؤرشف» ممكنًا دون إلغاء الأرشفة |
| 4 | الحالات الأخرى | رفض `CANCELED` على أي طرف عند الإنشاء (`Relations.DocumentCancelled`)؛ المسودة والمسجلة والمتحركة مقبولة؛ الحذف لا يعتمد على حالة الطرف الآخر | الرد غالبًا يُربط وهو مسودة؛ والملغاة لا قيمة لربطها |
| 5 | سبب الحذف | **إجباري** (حد 500)، واختياري `Notes` عند الإنشاء | إزالة رابط «يلغي» أو «نسخة عن» تصحيح يجب تبريره، كما في إلغاء الأرشفة |
| 6 | الطرف المخفي في الرسم البياني | تُحذف العقد والحواف التي لا يملك المستخدم `VIEW` عليها ولا يُعبَر خلالها، ويُعاد `hiddenRelationCount` فقط | لا تسرّب لعنوان ولا لبنية الرسم؛ والعدد يتيح للواجهة تنبيه المستخدم |
| 7 | تعديل الملاحظات | **خارج النطاق**؛ يُحذف الرابط ويُعاد إنشاؤه. يبقى `RelationActionType.Updated` غير مستخدم | لا يوجد في الدومين تعديل، ولا طلب فعلي |
| 8 | أثر `Supersedes` | معلوماتي فقط، **لا يغيّر حالة الوثيقة الملغاة بالتعويض** | تغيير الحالة يحتاج قواعد دورة حياة مستقلة |

## 3. المسارات

| المسار | الصلاحية | فحص الوصول |
| --- | --- | --- |
| `GET /api/documents/{documentId}/relations` | `DOCUMENT_VIEW` | `VIEW`؛ العقد المخفية تُحذف وتُعدّ |
| `GET /api/documents/{documentId}/relations/graph?depth=` | `DOCUMENT_VIEW` | `VIEW` على الجذر، وتصفية SQL لبقية العقد |
| `GET /api/documents/{documentId}/relations/events` | `DOCUMENT_VIEW` | `VIEW`؛ يشمل المحذوفة |
| `POST /api/documents/{documentId}/relations` | `DOCUMENT_EDIT` | `DOWNLOAD` على المسار + `VIEW` على الهدف |
| `DELETE /api/documents/{documentId}/relations/{relationId}` | `DOCUMENT_EDIT` | `DOWNLOAD` على المسار |

- جسم الإنشاء: `targetDocumentId`, `relationTypeId`, `notes?`. وثيقة المسار هي «المصدر» كما يراه المستخدم؛ التوحيد (`NormalizeEndpoints`) يحدث داخل الدومين.
- الحذف يحمل `reason` (إجباري). اتّباع اصطلاح `DocumentAccessGrantsController.Revoke` (`HttpDelete` على `{id}`)؛ يُحسم في المرحلة 3 هل يُمرَّر السبب في الجسم أم كمعامل استعلام، بحسب ما يقبله أداة OpenAPI والاختبارات الحالية.
- رابط يخص وثيقة المسار ولا يخصها (مصدر/هدف آخر) يعود 404 `Relations.RelationNotFound`.
- العمق الافتراضي للرسم 2 والأقصى 3، والحد الأقصى للعقد 100 (يُقتطع ويُعلَم بحقل `truncated`).

## 4. القواعد

### 4.1 الإنشاء
ترتيب الفحص: وثيقة المسار موجودة ← وصول `DOWNLOAD` ← الأرشفة ← الوثيقة الهدف موجودة ← وصول `VIEW` ← الحالة (§2.4) ← النوع موجود وفعّال ← ربط ذاتي ← تكرار ← دورة ← حفظ.
- المعالجة ذرية داخل `TransactionPipelineBehavior`: صف الرابط + `DocumentRelationEvent(Created)` + صف التدقيق.
- التكرار: فحص مسبق يعيد `Relations.DuplicateRelation`، والفهرس الفريد يحمي من السباق ويُحوَّل إلى 409 كما في بحث 07 الافتراضي.
- الدورة: للأنواع ذات `PreventCycles` فقط، وفي نطاق النوع نفسه. يُحسب `wouldIntroduceCycle` بوصول من الهدف إلى المصدر عبر حواف النوع (CTE تكراري محدود بعمق 50). أنواع متماثلة بلا منع دورات (`Related`) لا تُفحص.
- القفل: `sp_getapplock` بمفتاح `relations-cycle:{typeId}` ضمن معاملة الأمر، يؤخذ قبل حساب الدورة. وأمر أنواع بلا `PreventCycles` لا يأخذه.
- الأخطاء الجديدة تُضاف إلى `DomainErrors.Relations` (§5 المرحلة 0).

### 4.2 الحذف
- الإزالة الفعلية للصف، مع `DocumentRelationEvent(Removed)` يحمل الأطراف والنوع والسبب، وحدث الدومين `DocumentRelationRemovedEvent` ليظهر في التدقيق.
- تعيد الحذف نفسه للمعرف نفسه 404 (لا idempotency).

### 4.3 القراءة
- `ListDocumentRelationsQuery`: روابط الوثيقة المباشرة، كل صف يحمل `direction` (`Outgoing` | `Incoming` | `Symmetric`) نسبةً لوثيقة المسار، والنوع بعلمَيه `isSymmetric` و`preventCycles` (لأن lookup الأنواع العام لا يعرضهما)، وملخص الوثيقة الأخرى (العنوان، الرقم الرسمي، الحالة، مؤرشفة؟). التسمية العكسية («ردّ عليه») تؤلفها الواجهة؛ لا تُضاف أعمدة جديدة للـ lookup.
- `GetDocumentRelationsGraphQuery`: BFS على مستويات، كل مستوى باستعلام SQL واحد يطبّق `DocumentViewAccessQuery.WhereViewableBy` على العقد قبل التوسع منها، فلا يُعبَر عبر عقدة مخفية.
- `ListDocumentRelationEventsQuery`: السجل الدائم، الأحدث أولًا، مرقّم.

## 5. المراحل

### المرحلة 0 — Domain
- `DocumentRelation.Remove(actorUserId, reason, utcNow)` يرفع `DocumentRelationRemovedEvent`، مع التحقق من السبب.
- حدود الطول في `DocumentRelation.Create` (`Notes`) و`DocumentRelationEvent.Record` (`Reason`).
- أخطاء `Relations.*` الجديدة: `RelationNotFound`, `RelationTypeNotFound`, `ReasonRequired`, `ReasonTooLong`, `NotesTooLong`, `DocumentCancelled`, `GraphDepthInvalid`.
- توسيع `IDocumentRelationRepository`: `GetTypeByIdAsync`, `ExistsAsync(source, target, typeId)`, `WouldCreateCycleAsync(typeId, source, target)`, `ListByDocumentAsync`, `ListEventsByDocumentAsync` (مرقّم), `GetNeighborsAsync` للرسم, `Add(DocumentRelationEvent)`.

### المرحلة 1 — Infrastructure
- `DocumentRelationRepository` وتسجيله في `DependencyInjection`، مع `WouldCreateCycleAsync` (CTE + قفل) و`GetNeighborsAsync` المقيّد بـ `WhereViewableBy`.
- **تحقق قبل البدء:** أن `TamperEvidentAuditInterceptor` يقرأ أحداث الدومين من كيان بحالة `Deleted` ويقدّمها على `MapEntityChange` (يستخدم `entries` ويستثني الكيانات المعالجة)، وإلا يُصحَّح ليُسجَّل حذف الرابط بحدث الإزالة لا بتغيير كيان عام.
- Migration `Add_Relations_Constraints`: FK لـ `DOCUMENT_RELATIONS` (`SourceDocumentId`, `TargetDocumentId` → الوثائق، `CreatedByUserId` → المستخدمون)؛ FK لـ `DOCUMENT_RELATION_EVENTS` (`SourceDocumentId`, `TargetDocumentId`, `ActorUserId`) **بلا FK على `RelationId`**؛ قيد `CHECK (SourceDocumentId <> TargetDocumentId)`؛ فهرس `IX_DOC_RELATIONS_TARGET (TargetDocumentId)`؛ فهرس أحداث `(SourceDocumentId, OccurredAt)` و`(TargetDocumentId, OccurredAt)`.

### المرحلة 2 — Application (`Features/Relations/`)
- `CreateDocumentRelationCommand`, `RemoveDocumentRelationCommand` مع validators.
- `ListDocumentRelationsQuery`, `GetDocumentRelationsGraphQuery`, `ListDocumentRelationEventsQuery` والـ responses (`DocumentRelationResponse`, `RelationGraphResponse` بعقده وحوافه، `DocumentRelationEventResponse`).
- كل المعالجات تُمرّر `IUnitOfWork.UtcNow` إلى الدومين.

### المرحلة 3 — Server
- `DocumentRelationsController` و`Contracts/Relations/` (`CreateDocumentRelationRequest`, `RemoveDocumentRelationRequest`).
- ملخص ووصف ومثال لكل مسار ومعامل (`OpenApiDocumentationTests` يفشل بدونها؛ `add-endpoint-docs.mjs` يساعد).
- `AuditActionMiddleware.ResolveModule`: إضافة `"DocumentRelations" => "RELATIONS"` (الأفعال `Create`/`Remove` مصنّفة أصلًا).
- حسم اصطلاح جسم/استعلام سبب الحذف (§3).

### المرحلة 4 — Tests
- **Domain:** الربط الذاتي، النوع غير الفعّال، توحيد الطرفين للمتماثل، منع الدورة، `Remove`، حدود الطول.
- **Application:** ترتيب الفحوص الثمانية للإنشاء، رفض المؤرشفة للمسار وقبول المؤرشفة كهدف، رفض الملغاة، رابط وثيقة أخرى يعود 404، رفض الحذف بلا سبب.
- **حالات حرجة:**
  - دورة مباشرة وغير مباشرة (`A→B→C→A`) لكل نوع ذي `PreventCycles`، وقبول الدورة العابرة بين نوعين مختلفين، وقبول `Related` المتماثل.
  - **سباق الدورات:** `A→B` و`B→A` المتوازيان ينجح أحدهما فقط والآخر 409 لا 500.
  - تكرار متزامن يعيد 409 لا 500، ورابط متماثل معكوس الاتجاه يُعدّ تكرارًا.
  - **تطابق الرسم مع الوصول:** كل عقدة في الرسم تجتاز `CanAccessDocumentAsync(VIEW)` والعكس، وعقدة `SECRET` لا تظهر بالنطاق وحده، ولا يُعبَر من خلالها، و`hiddenRelationCount` صحيح.
  - حذف رابط يُنتج صف تدقيق واحدًا بحدث الإزالة (لا صفّ كيان إضافي)، ويبقى `DocumentRelationEvent` بعد الحذف.
  - سلسلة التدقيق تبقى سليمة بعد الإنشاء والحذف.
- **Persistence:** الفهارس الفريدة وFK وقيد CHECK على SQL Server، وصحة ترجمة CTE والرسم.
- **Server:** الصلاحيات (403)، وOpenAPI، وتصنيف التدقيق والوحدة `RELATIONS`.

### المرحلة 5 — التوثيق
- `README.md` لكل مجلد تغيّر (Contents + Implementation status + Change Log)، وREADME جديد لـ `Features/Relations/` و`Contracts/Relations/`.
- `InstitutionalDiwan.Application/Features/README.md` (أسطر الروابط وجدول الحالة) و`README.md` الجذر (صف المودول 08).
- `institutionaldiwan.front/knowledge.md`: المسارات الجديدة بوسم «باك إند فقط، غير مطبّق في الواجهة»، وإزالة 08 من «Modules without API».
- `CLAUDE.md`: تحديث «Backend modules 01–07 and 09» لتشمل 08. لا تغيير في الصلاحيات ولا في `permissions.ts`.

### لاحقًا — خطة الواجهة (منفصلة)
لوحة الروابط في تفاصيل الوثيقة، إضافة/حذف رابط، عرض الرسم عبر `mermaid-view`، ترجمة أخطاء `Relations.*` إلى العربية، وأدلة الوضع التعليمي.

## 6. التحقق

`dotnet build InstitutionalDiwan.slnx` و`dotnet test InstitutionalDiwan.slnx` نظيفان؛ ثم على الـ API الحي (Scalar) بقاعدة مؤقتة: وثيقتان مسجلتان ← ربط «ردّ على» ← محاولة العكس (409 دورة) ← ربط «مرتبط» بالاتجاهين (409 تكرار) ← الرسم بحساب `admin` ثم `user` للتأكد من الإخفاء ← حذف بسبب ← مراجعة `/api/audit-logs` وسجل الأحداث ← أرشفة الوثيقة المصدر ورفض الربط (409) ← قبول الربط بها كهدف. وطلبات متوازية للدورة والتكرار تتحقق من غياب أي 500، وسلسلة التدقيق تبقى سليمة.

## 7. خارج النطاق

- **الواجهة** بأكملها.
- **تعديل ملاحظات الرابط** (القرار 7) والتسميات العكسية داخل الـ lookup.
- **أثر `Supersedes` على حالة الوثيقة** (القرار 8)، وقيود مثل «وثيقة واحدة تلغي واحدة».
- **الربط الجماعي**، وتصدير الرسم، وفلترة بحث الأرشيف حسب الروابط.
- **إشعار الأطراف عند الربط:** يتبع المودول 10 (Notifications) عندما يُبنى.
- **تدقيق الطرف الهدف:** قيد معروف (الثغرة 6).
