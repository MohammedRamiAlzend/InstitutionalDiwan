# خطة الوحدة 08 — الروابط بين الوثائق (الواجهة الأمامية)

> **الحالة: منفّذة (2026-11-03) — `npm run lint` و`npm run build` ناجحان؛ لم يُتحقق منها بعد على API حي (المرحلة 5 اليدوية معلّقة).** الواجهة الخلفية منفّذة ومُلتزَم بها (`a32f944`).

## 1. الهدف

تمكين المستخدم من **ربط وثيقة بوثائق أخرى** (ردّ على / ذات صلة / تحلّ محل / مكرّرة من)، واستعراض الروابط الصادرة والواردة، ورؤية **مخطط الروابط** (رسم Mermaid + عرض شجري)، وإزالة رابط بسبب مسجَّل، ومتابعة **سجل أحداث الروابط** — كل ذلك من صفحة تفاصيل الوثيقة داخل تبويب جديد «الروابط».

## 2. الحالة الراهنة

| العنصر | الحالة |
| --- | --- |
| API الوحدة 08 | ✅ جاهز: `GET/POST /api/documents/{id}/relations`، `GET .../graph?depth=`، `GET .../events`، `DELETE .../{relationId}` (جسم JSON `{reason}`) |
| الصلاحيات | القراءة `DOCUMENT_VIEW`، الكتابة `DOCUMENT_EDIT` (+ وصول فعلي للوثيقة؛ الوثائق المخفية تُحذف وتُحتسب في `hiddenRelationCount`) |
| قائمة البحث `document-relation-types` | ✅ موجودة في الخادم، غير مسجّلة في `LOOKUP_SLUGS` |
| ثابت `AUDIT_ENTITY_TYPE.DocumentRelation` | ✅ موجود مسبقًا في `shared/config/lookupIds.ts` |
| `MermaidView` + نمط `toMermaid` + مبدّل مخطط/شجرة | ✅ جاهز (`shared/ui/mermaid-view.tsx`، `entities/department/model/toMermaid.ts`) |
| منتقي وثيقة (Document picker) | ❌ غير موجود — يلزم بناؤه فوق `useDocuments` |
| كتالوج أخطاء `Relations.*` بالعربية | ❌ غير موجود |
| أدلة الوضع التعليمي للروابط | ❌ غير موجودة |
| تبويب في `DocumentDetailsPage` | ❌ غير موجود |

## 3. الفجوات المطلوب سدّها

1. أنواع وعميل API واستعلامات للقراءة (entity جديد).
2. نموذج إنشاء رابط (اختيار وثيقة هدف + نوع + ملاحظات) وحوار إزالة بسبب (features جديد).
3. لوحة الروابط: جدول كامل (full grid) + مخطط + سجل أحداث (widget جديد).
4. تبويب «الروابط» في صفحة التفاصيل مع التحكم بالظهور والتعطيل حسب الصلاحية وحالة الوثيقة.
5. رسائل خطأ عربية، وأدلة تعليمية، وتحديث الوثائق.

## 4. القرارات المعمارية (FSD الصارم)

| الطبقة | الشريحة | المحتوى |
| --- | --- | --- |
| `shared/config` | `lookupIds.ts` | `DOCUMENT_RELATION_TYPE = { ReplyTo: 1, Related: 2, Supersedes: 3, DuplicateOf: 4 }` و`RELATION_DIRECTION` (`Outgoing/Incoming/Symmetric`)، نفس قيم الخادم حرفيًا |
| `shared/config/guides` | `relations.ts` | كتالوج الأدلة (مفاتيح حرفية) + تسجيله في `index.ts` |
| `shared/api` | `errorMessages.ts` | مفاتيح `Relations.*` + `Correspondence.DocumentArchived` موجود مسبقًا |
| `entities` | `document-relation` | الأنواع، `relationKeys`، `useDocumentRelations`، `useRelationGraph`، `useRelationEvents`، `relationsToMermaid` / `relationGraphToTree`، `RelationTypeBadge`، `RelationDirectionLabel` |
| `entities/lookup` | `constants.ts` | `LOOKUP_SLUGS.DocumentRelationTypes = 'document-relation-types'` + `LOOKUP_FALLBACKS` عربية |
| `features` | `document-relation-manage` | `CreateRelationDialog`، `RemoveRelationDialog`، `DocumentPicker`، مخططات zod، `useCreateRelation`، `useRemoveRelation` |
| `widgets` | `document-relations-panel` | الجدول + عرض المخطط/الشجرة + سجل الأحداث + تنبيه «روابط مخفية» |
| `pages` | `documents` | تبويب «الروابط» في `DocumentDetailsPage` (يبقى الملف أقل من الحد؛ المنطق في الـ widget) |

قرارات تفصيلية:

- **اتجاه الرابط**: الخادم يعيد `direction` نسبةً إلى الوثيقة المعروضة. الواجهة تركّب النص العكسي (مثال: نوع «ردّ على» + وارد ⇒ «يُردّ عليها من»؛ «تحلّ محل» + وارد ⇒ «حلّت محلها»؛ «ذات صلة» و«مكرّرة من» متماثلة). يُجمَّع هذا في `RelationDirectionLabel` فقط.
- **منتقي الوثيقة الهدف**: يعيد استخدام `useDocuments` من `entities/document` (بحث نصي debounced عبر `useDebouncedValue`، صفحة صغيرة 10)، ويستبعد الوثيقة الحالية والوثائق الملغاة، ويعرض العنوان + `OfficialNumberTag` (LTR) + الحالة. الشرائح لا تستورد بعضها: المنتقي داخل `features/document-relation-manage` ويستورد `@/entities/document` فقط عبر الـ barrel.
- **الاحتمالات المرفوضة من الخادم** (دورة، مكرر، ذاتي، وثيقة ملغاة، الوثيقة الأولى مؤرشفة) تُترجم إلى رسائل عربية عبر `error.code`؛ لا نكرر الفحص في العميل سوى الذاتي والمكرر الواضح المعروض في الجدول لتحسين التجربة.
- **المخطط**: عمق 1–3 يُحفظ في عنوان الصفحة (`?tab=relations&depth=2&view=graph`)، حد 100 عقدة و`truncated` يُعرض كتنبيه. النقر على عقدة (`onNodeClick`) يفتح تلك الوثيقة. العرض الشجري بديل ثابت (إتاحة + حالة فشل Mermaid)، ومعرّفات العقد تسلسلية `n1…nK` مع خريطة إلى GUID كما في `toMermaid` للأقسام.
- **الصلاحيات**: التبويب ظاهر لمن لديه `DOCUMENT_VIEW`؛ أزرار الإنشاء/الإزالة ومدخلا القائمة السياقية تظهر مع `DOCUMENT_EDIT`، وتُعطَّل (مع تلميح عربي) عندما تكون الوثيقة **مؤرشفة** أو **ملغاة** (`canManage = canEdit && !isArchived && !isCanceled`). الوثيقة الهدف المؤرشفة مسموحة.
- **الجدول**: عقد الجدول الكامل — شريط مرشحات (النوع، الاتجاه)، بحث، ترتيب، ترقيم، مُختار الأعمدة (`diwan.table.document-relations.columns.v1`)، وقائمة سياقية (فتح الوثيقة، نسخ الرقم، إزالة الرابط). الحالة في `useTableState` / URL، وبيانات الخادم في React Query فقط.
- **سجل الأحداث**: قسم قابل للطي داخل التبويب يعتمد `useRelationEvents`، بنفس نمط تبويب الأرشفة (هيكل تحميل / خطأ + إعادة محاولة / حالة فارغة منقّطة).
- **التحديث بعد الكتابة**: `useCreateRelation`/`useRemoveRelation` تُبطل `relationKeys.all(documentId)` (القائمة + المخطط + الأحداث) وتبطل `documentKeys.detail(documentId)`؛ وتُبطل كذلك مفاتيح الوثيقة المقابلة لأن الرابط ظاهر من الجهتين.
- **العربية أولًا**: CSS منطقي فقط، لا ألوان hex، الأرقام الرسمية والمعرّفات في جزر `dir="ltr"`.

## 5. المراحل

### المرحلة 0 — الأساس (config + lookup + أخطاء)
- إضافة `DOCUMENT_RELATION_TYPE` و`RELATION_DIRECTION` في `shared/config/lookupIds.ts` وتصديرها.
- `LOOKUP_SLUGS.DocumentRelationTypes` + `LOOKUP_FALLBACKS['document-relation-types']` (ردّ على، ذات صلة، تحلّ محل، مكرّرة من).
- مفاتيح `Relations.RelationNotFound / RelationTypeNotFound / ReasonRequired / ReasonTooLong / NotesTooLong / DocumentCancelled / GraphDepthInvalid` بالعربية، مع بقاء مفاتيح المواقع الحالية مثل `Correspondence.DocumentArchived`، وأي كود تحقّق عام (Numbering/Validation) مستخدم للعلاقات المكررة/الدورة بعد تأكيده من الخادم.
- **القبول**: `npm run build` و`npm run lint` ناجحان؛ لا استدعاءات جديدة.

### المرحلة 1 — شريحة `entities/document-relation` (القراءات)
- `model/types.ts` مطابق لـ `RelationResponses.cs`: `DocumentRelationResponse` (`direction`, `relationTypeId`, `notes`, `createdAt`, الوثيقة المقابلة `RelatedDocumentSummary`)، `DocumentRelationsResponse` (`items`, `hiddenRelationCount`)، `RelationGraphResponse` (`nodes`, `edges`, `truncated`)، `DocumentRelationEventResponse`.
- `api/relationApi.ts` (`list`, `graph(depth)`, `events`) عبر `httpClient`.
- `model/queries.ts`: `relationKeys` + ثلاث hooks مع `enabled` و`staleTime`.
- `model/toMermaid.ts` و`model/toTree.ts` (مع الخريطة `nodeId → documentId`)، وتنقية التسميات كما في `departmentTreeToMermaid`.
- `ui/RelationTypeBadge`, `ui/RelationDirectionLabel`، وإضافة README و`index.ts`.
- **القبول**: بناء ناجح؛ فحص يدوي لاستجابة حقيقية من الـ API.

### المرحلة 2 — شريحة `features/document-relation-manage` (الكتابات)
- `DocumentPicker` (بحث + نتائج بلوحة مفاتيح، يستبعد الحالية).
- `CreateRelationDialog`: نوع الرابط من القائمة + ملاحظات (≤ الحد من الخادم) + zod؛ تلميح تعليمي «تحلّ محل» معلوماتي فقط ولا يغيّر حالة الوثيقة.
- `RemoveRelationDialog`: سبب إلزامي (≤ الحد) عبر `ConfirmDialog`/نموذج، يرسل `DELETE` بجسم JSON.
- `api/relationMutations.ts` بإبطال المفاتيح الموضّحة أعلاه، ورسائل خطأ عبر `toArabicErrorMessage(error)`.
- **القبول**: إنشاء/إزالة ناجحان ويحدّثان الجدول والمخطط بلا إعادة تحميل؛ كل خطأ خادم (مكرر، دورة، مؤرشفة، ملغاة) يُظهر رسالة عربية صحيحة.

### المرحلة 3 — `widgets/document-relations-panel`
- جدول كامل (أعمدة: النوع، الاتجاه، الوثيقة المقابلة + رقمها، حالتها، ملاحظات، التاريخ) مع المرشحات والبحث والترتيب والترقيم ومُختار الأعمدة والقائمة السياقية المفلترة بالصلاحيات.
- مبدّل «جدول / مخطط / شجرة» مع منتقي العمق (1–3) و`truncated`، وتحميل Mermaid الكسول.
- تنبيه «يوجد N رابط/روابط لوثائق لا تملك صلاحية الاطلاع عليها» عندما `hiddenRelationCount > 0` (دون أي كشف لعناوين).
- قسم سجل الأحداث.
- **القبول**: مرشحات/عمق/عرض محفوظة في العنوان؛ لوحة المفاتيح تصل إلى كل عنصر؛ لا ملف يتجاوز ~250 سطرًا (تُقسَّم الأجزاء الفرعية).

### المرحلة 4 — تكامل الصفحة + الوضع التعليمي
- تبويب `{ key: 'relations', label: 'الروابط' }` في `DocumentDetailsPage` + فرع في `resolveTab` + `GuideRegion id="document-details.tab-relations"` + `GuideChip` لإجراء «إضافة رابط».
- `relations.ts` في الأدلة: `relations.grid`, `relations.types`, `relations.direction`, `relations.graph`, `relations.hidden`, `relations.events` + `RELATIONS_GUIDE` (pageId) وتسجيله؛ `npm run check:guides` يمر.
- تعطيل أزرار الكتابة مع سبب عربي للوثائق المؤرشفة/الملغاة.
- **القبول**: `npm run lint` و`npm run build` بلا أخطاء.

### المرحلة 5 — التحقق والوثائق
- تشغيل الـ SPA على API حقيقي بعد تطبيق الترحيل `20261103090137_Add_Relations_Constraints` على قاعدة التطوير.
- مصفوفة أدوار: `admin` (نطاق النظام) و`user` (نطاق القسم): إنشاء/إزالة، وثيقة مخفية، مؤرشفة، ملغاة، دورة، رابط مكرر، عمق 3 مع اقتطاع.
- فحص بصري: RTL كامل، الوضعان الفاتح/الداكن، الجوال، لوحة المفاتيح، قارئ الشاشة (`aria-label` للمخطط).
- `dotnet build` / `dotnet test` للتأكد من عدم الانحدار (لا تغيير خلفي متوقع).
- تحديث الوثائق في نفس التغيير: README لكل مجلد لُمس (Contents/Status/Change Log)، `institutionaldiwan.front/knowledge.md` (حذف «Backend only: 08»)، README الجذر وحالة الوحدات، `CLAUDE.md` / `AGENTS.md` إن تغير نص الحالة، وخطة الخلفية إن لزم.

## 6. المخاطر والملاحظات

- **لا توجد صلاحية لوحدة 08 مستقلة**: التبويب يعتمد `DOCUMENT_VIEW/EDIT`؛ لا تُضاف رموز صلاحيات جديدة (يبقى العدد 17).
- **الأداء**: المخطط بعمق 3 قد يبلغ 100 عقدة؛ يُحمَّل عند طلب العرض فقط (`enabled` بحسب العرض النشط).
- **نص Mermaid**: تنقية التسميات إلزامية (العناوين عربية حرة)، والحد 20 ألف حرف في `MermaidView`.
- **ما لم يُتحقق منه بعد في الخلفية**: مسار `sp_getapplock` المتزامن غير مغطى باختبار؛ يُراقَب يدويًا في المرحلة 5.

## 7. خارج النطاق

- وحدات 08 الأخرى غير المرتبطة بالروابط (لا يوجد API)، ووحدة 10.
- تغيير حالة الوثيقة عند «تحلّ محل» (معلوماتي فقط في الخادم).
- تحرير ملاحظات رابط قائم (غير مدعوم في API: الإزالة وإعادة الإنشاء).
- اختبارات آلية للواجهة (لا يوجد مشغّل اختبارات؛ البوابات هي `build` و`lint` والتشغيل الفعلي).
