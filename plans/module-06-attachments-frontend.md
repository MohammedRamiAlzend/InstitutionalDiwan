# خطة الموديول 6 — المرفقات (واجهة SPA)

> **الحالة:** 🔄 قيد التنفيذ (2026-11-01) — Phase 0 وPhase 1 وPhase 2 وPhase 3 وPhase 4 منفذة
> والبوابات الأربع خضراء؛ بانتظار المراجعة قبل Phase 5. تحقّق المتصفح لتبويب الملفات (Phase 2) وللرفع
> الذرّي (Phase 3) وللإصدارات والأرشفة والتحقق (Phase 4) أُلغي بطلب المستخدم.
> **النطاق:** إغلاق واجهة `institutionaldiwan.front` للموديول 6 (ملفات الوثيقة: رفع متعدد الأجزاء،
> إصدارات، تنزيل، أرشفة، تحقق من بصمة SHA-256) وفق عقد التفاعل المعتمد، **بلا أي تغيير خلفي**.
> **الخلفية:** خلفية الموديول 6 مكتملة ومختبرة: ثماني نقاط نهاية في `DocumentFilesController`
> (رفع ذرّي، رفع إصدار، أرشفة، قائمتان، تنزيل، تحقق) و`dotnet test` = 487. الشرائح الأمامية الثلاث
> (`entities/document-file`، `features/attachment-upload`، `widgets/document-attachments-panel`)
> توثيق فقط، وتبويب «الملفات» في تفاصيل الوثيقة موجود ومعطّل بـ `available: false` وشارة «قريبًا».

---

## 1. الوضع الحالي المؤكد (تم فحصه)

| العنصر | الحالة |
| --- | --- |
| `POST /api/documents/{documentId}/files` (multipart ذرّي: `file` + `fileTypeId` + `title` + `isPrimaryScan` + `changeNotes`) | الخلفية جاهزة (`DocumentFilesController.cs:102-146`)؛ الواجهة ❌ |
| `POST /api/document-files/{fileId}/versions` | الخلفية جاهزة (`:165-203`)؛ الواجهة ❌ |
| `POST /api/document-files/{fileId}/archive` | الخلفية جاهزة (`:212-231`)، والأرشفة idempotent؛ الواجهة ❌ |
| `GET /api/documents/{documentId}/files` و`GET /api/document-files/by-document/{documentId}` | **متطابقان تمامًا** (`:279` يفوّض إلى `:240`)؛ مصفوفة بلا ترقيم ولا ترشيح، المؤرشف مشمول، الملفات تصاعديًا بـ `CreatedAt` والإصدارات تنازلية بـ `VersionNumber` |
| `GET /api/document-files/{fileId}/download?versionId=` | `FileContentResult` بنوع MIME المخزّن واسم الملف الأصلي مع دعم النطاقات (`:289-316`)؛ يتطلب مستوى وصول **DOWNLOAD** |
| `GET /api/document-files/{fileId}/verify?versionId=` | يعيد `recordedChecksumSha256` + `computedChecksumSha256` + `isMatch` (`AttachmentChecksumResponse.cs`) |
| `GET /api/lookups/document-file-types?includeInactive=` | يعيد `id/desc/descAr/isActive` **فقط** (`LookupEntryResponse.cs:6-11`) |
| مستويات الوصول لكل إجراء | الكتابة تشترط MANAGE (4)، القائمة/التحقق VIEW (1)، التنزيل DOWNLOAD (2) — فوق الصلاحية نفسها |
| الصلاحيات | لا صلاحية مخصّصة للمرفقات: الكتابة `DOCUMENT_EDIT` والقراءة `DOCUMENT_VIEW` (`Permissions.cs:10-12`)؛ الدور USER يملكهما معًا |
| رموز الأخطاء | 21 رمز `Attachments.*` في `DomainErrors.cs:181-204` + `Attachments.ChangeNotesTooLong` الموجود في المدقّات فقط |
| تبويب «الملفات» في `/documents/:documentId` | موجود ومعطّل (`pages/documents/ui/DocumentDetailsPage.tsx:36`، `:41`، `:44-54`) |
| `Correspondence.DocumentAccessDenied` عربيًا | ✅ موجود مسبقًا (`shared/api/errorMessages.ts:99`) |

## 2. الثغرات المكتشفة (أساس الخطة)

| # | الثغرة | الدليل |
| --- | --- | --- |
| 1 | الشرائح الثلاث بلا أي كود — READMEs فقط، وجداول حالتها «Code ❌» | `entities/document-file/`، `features/attachment-upload/`، `widgets/document-attachments-panel/` |
| 2 | لا دعم للرفع متعدد الأجزاء ولا للتنزيل الثنائي في عميل HTTP؛ الاتفاق موثّق لكنه غير منفّذ | `shared/api/httpClient.ts` (axios + معترضات فقط)، `shared/api/README.md:45-46` |
| 3 | لا مساعد لتحليل `Content-Disposition`، وأسماء الملفات الأصلية قد تكون عربية فتحتاج `filename*=UTF-8''` | `DocumentFilesController.cs:311-315` |
| 4 | خطأ التنزيل يعود كـ `blob` نصّه `ProblemDetails` JSON، فلا يمرّ في `toApiError` وتظهر رسالة عامة بدل العربية | `shared/api/problemDetails.ts:23-40` |
| 5 | لا رمز عربي لأي من رموز `Attachments.*` (ومنها `ChangeNotesTooLong` غير المعرّف في `DomainErrors.cs`) | `shared/api/errorMessages.ts:9-172` |
| 6 | حدود كل نوع (20MB مسح ضوئي، 50MB مرفق/أخرى، 2MB توقيع + الامتدادات المسموحة) غير مكشوفة في استجابة القاموس، فلا تحقق مسبق ممكن من البيانات الحيّة | `LookupEntryResponse.cs:6-11` مقابل `AttachmentsLookupSeeders.cs:27-45` |
| 7 | الواجهة لا تستطيع معرفة مستوى وصول المستخدم للوثيقة (لا حقل في أي استجابة)، بينما الخادم يشترط MANAGE للكتابة وDOWNLOAD للتنزيل | `Features/Attachments/*Handler.cs` (تقييم `IDocumentAccessEvaluator`) |
| 8 | بصمة SHA-256 في المتصفح موعودة ولم تُنفّذ | `shared/lib/README.md:12` («`crypto.ts` … ❌ pending Module 06») |
| 9 | لا slug لأنواع الملفات في `LOOKUP_SLUGS` ولا مرآة `LOOKUP_FALLBACKS` لها | `entities/lookup/model/constants.ts:48-69`، `:78-…` |
| 10 | `DocumentFileResponse.relativePath` (مسار التخزين الداخلي) مكشوف لأي حامل `DOCUMENT_VIEW` | `Application/Features/Attachments/DocumentFileResponse.cs:10` |

> ملاحظة عقدية: لا توجد نقطة نهاية لحذف ملف أو إصدار (الأرشفة هي النهاية الوحيدة)، ولا نقطة
> لإعادة تسمية الملف بعد إنشائه؛ لذلك لا تُصمَّم أي واجهة حذف أو تعديل عنوان.

## 3. قرارات معمارية

1. **FSD كما هو معتمد:** `pages → widgets → features → entities → shared`، وكل شريحة تُصدّر
   واجهتها العامة من `index.ts`، و`README.md` لكل مجلد يتحدّث مع الكود في نفس المرحلة.
2. **لا تغيير خلفي — ومرآة ثابتة للحدود (قرار مستخدم 2026-11-01):** حدود الأنواع تُنسخ في
   `shared/config` مطابقةً لـ `AttachmentsLookupSeeders.cs:27-45`، على غرار `LOOKUP_FALLBACKS`
   الموجود. الخادم يبقى الحكم، وأخطاؤه تُعرض بالعربية كشبكة أمان.
3. **البصمة المحلية مضمّنة (قرار مستخدم 2026-11-01):** `shared/lib/crypto.ts` عبر
   `crypto.subtle.digest('SHA-256', buffer)`، تُحسب عند اختيار الملف وتُقارن بالمخزنة بعد الرفع
   وعبر `/verify`، فيصبح إجراء «التحقق» ذا معنى مزدوج (نقل سليم + تخزين سليم).
4. **المسارات الوثائقية أولًا:** `POST/GET /api/documents/{id}/files` للرفع الذرّي والقائمة
   (حسب `knowledge.md:182-199`: «Prefer the document-scoped routes for the UI»)، والمسارات العامة
   تُستخدم فقط لما لا بديل وثائقي له: `versions`، `archive`، `download`، `verify`.
5. **الرفع الذرّي هو المسار الوحيد للإنشاء:** `POST /api/document-files` (بلا ملف) لا يُستهلك في
   الواجهة؛ الخلفية أبقته «for compatibility» فقط.
6. **القائمة غير مصفّحة ⇒** `shared/ui/data-table` بوضع `client` ومعرّف `document-files` (العقد
   الموثّق في `widgets/document-attachments-panel/README.md:10`)، مع `useTableState` ومرشحات في الرابط.
7. **الصفوف = الملفات، والإصدارات في حوار مستقل** (لا صفوف متوسعة، لأن الشبكة المشتركة لا تدعم
   التوسعة)؛ الحوار يسرد الإصدارات تنازليًا مع تنزيل لكل إصدار.
8. **الصلاحيات تُخفي الأزرار ولا تُعطّلها** (`DOCUMENT_EDIT` للكتابة)، أما مستوى الوصول لكل وثيقة
   فغير معروف مسبقًا (ثغرة 7) — لذا لا تُعطَّل الأزرار احترازيًا، وتُترجم استجابة الخادم
   (`Correspondence.DocumentAccessDenied`) عربيًا عند وقوعها.
9. **التحقق المسبق في zod:** الامتداد ضمن المسموح للنوع، والحجم ≤ حد النوع، والعنوان ≤ 300،
   وملاحظات التغيير ≤ 500 — برسائل عربية داخل المخطط كما في `features/document-create/model/schemas.ts`.
10. **`isPrimaryScan` يُرسل صراحةً** (`false` عند عدم الاختيار) لأن الحقل غير قابل للفراغ في توقيع
    الإجراء الخلفي.

## 4. المراحل

كل مرحلة تنتهي بـ `npm run lint` + `npm run build` (و`dotnet build`/`dotnet test` للتأكد من عدم
الانحدار)، مع تحديث READMEs المتأثرة، ثم **توقف للمراجعة** قبل المرحلة التالية. Phase 0 وPhase 1
بلا واجهة مرئية فتُقبلان بالبوابات فقط؛ من Phase 2 يلزم التحقق في المتصفح.

### Phase 0 — الأساس المقروء (entities/document-file + الكتالوج العربي + مرآة الأنواع)

- **المخرجات:**
  - `shared/config/lookupIds.ts`: `DOCUMENT_FILE_TYPE = { Scan: 1, Attachment: 2, Signature: 3, Other: 4 }`.
  - `shared/config/attachmentLimits.ts`: `DOCUMENT_FILE_TYPE_LIMITS` — لكل نوع `maxBytes` و`allowedExtensions`
    مطابقة للبذر (Scan: 20MB و`.pdf,.jpg,.jpeg,.png,.tif,.tiff`؛ Attachment: 50MB و`.pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.tif,.tiff,.zip`؛
    Signature: 2MB و`.png,.jpg,.jpeg,.svg`؛ Other: كالـ Attachment)، مع تعليق يشير إلى
    `AttachmentsLookupSeeders.cs:27-45` وإلى أن الخادم هو المرجع، وثابت `MAX_UPLOAD_BYTES ≈ 51MB`
    المطابق لـ `DocumentFilesController.cs:27`، ودالة `fileTypeLimits(fileTypeId)` بسلوك احتياطي آمن
    (أشد الحدود) للنوع غير المعروف.
  - `shared/lib/format.ts`: `formatFileSize(bytes)` (أرقام لاتينية + وحدات عربية: بايت/ك.ب/م.ب)
    لمقارنة `formatNumber`/`formatShortId` الموجودتين.
  - `entities/lookup/model/constants.ts`: إضافة `DocumentFileTypes: 'document-file-types'` إلى
    `LOOKUP_SLUGS` + مرآة `LOOKUP_FALLBACKS['document-file-types']` بأسماء عربية (مسح ضوئي/مرفق/توقيع/أخرى).
  - `shared/api/errorMessages.ts`: كتلة `// Module 06 — Attachments` بكل رموز `Attachments.*`
    الاثنين والعشرين (بما فيها `ChangeNotesTooLong`) مرتبة أبجديًا داخل الكتلة.
  - `entities/document-file/model/types.ts`: `DocumentFileResponse`، `DocumentFileVersionResponse`،
    `AttachmentChecksumResponse` بأسماء camelCase مطابقة للعقد حرفيًا.
  - `entities/document-file/api/documentFileApi.ts`: `listByDocument(documentId)` على
    `GET /api/documents/{documentId}/files`، و`verify({ fileId, versionId? })`.
  - `entities/document-file/model/queries.ts`: `documentFileKeys = { all: ['document-files'], byDocument(documentId), verify(fileId, versionId?) }`
    (كلها `as const`) + `useDocumentFiles(documentId, options?)` مع `enabled: Boolean(documentId)`
    و`placeholderData` و`staleTime` كما في `entities/document/model/queries.ts:32-56` + `useVerifyChecksum`
    معطّل افتراضيًا (يُستدعى عند الطلب).
  - `entities/document-file/ui/ChecksumBadge.tsx` (مطابقة/عدم مطابقة/غير محسوب) و`ui/FileArchivedBadge.tsx`
    (نشط/مؤرشف) و`ui/FileTypeLabel.tsx` إن لزم، ثم `index.ts`.
- **قبول:** `npm run lint` + `npm run build` أخضران؛ لا واجهة بعد — التبويب يبقى معطّلًا ولا تستدعي
  أي صفحة الخطافات الجديدة.
- **لمس الخلفية:** لا.
- **الحالة:** ✅ منفذة 2026-11-01 — `DOCUMENT_FILE_TYPE` في `shared/config/lookupIds.ts`؛
  `shared/config/attachmentLimits.ts` (`DOCUMENT_FILE_TYPE_LIMITS` مطابقة لـ
  `AttachmentsLookupSeeders.cs:37-45`، `MAX_UPLOAD_BYTES = 51MB`، `fileTypeLimits`)؛ `formatFileSize`
  في `shared/lib/format.ts`؛ slug `document-file-types` ومرآته العربية في
  `entities/lookup/model/constants.ts`؛ كتلة `// Module 06 — Attachments` بـ 22 رمزًا في
  `shared/api/errorMessages.ts`؛ و`entities/document-file` كاملًا (`model/types.ts`،
  `api/documentFileApi.ts`، `model/queries.ts`، `ui/ChecksumBadge.tsx`، `ui/FileArchivedBadge.tsx`،
  `index.ts`). READMEs المحدّثة: `shared/config`، `shared/lib`، `shared/api`، `entities/lookup`،
  `entities`، و`entities/document-file`. التبويب بقي معطّلًا ولا تستدعي أي صفحة الخطافات الجديدة.
- **انحراف عن الخطة:** `fileTypeLimits` يعيد `null` للنوع غير المعروف بدل «أشد الحدود» كما نصّت
  الخطة. السبب: تخمين أشد نوع (توقيع: 2MB وأربعة امتدادات) كان سيحجب منعًا نهائيًا رفعًا مشروعًا
  تحت نوع يُبذر لاحقًا بلا مخرج للمستخدم، بينما `null` لا يوسّع أي حد لأنه لا يُعلن حدًا أصلًا
  ويترك القرار للخادم.
- **لم تُنفّذ:** `ui/FileTypeLabel.tsx` (كانت «إن لزم» في الخطة) — `LookupLabel` في
  `entities/lookup` يغطي التسمية العربية.
- **قيود التحقق:** لا واجهة في هذه المرحلة، فالتحقق اقتصر على البوابات وعلى فحص `formatFileSize`
  بمنطق مكافئ في Node (1024 → «1 ك.ب»، 1536 → «1.5 ك.ب»، 50MB → «50 م.ب»، `null` → «—»)، وعلى
  `diff` أثبت أن مفاتيح الكتالوج العربي مطابقة تمامًا لمجموعة رموز `Attachments.*` في
  `DomainErrors.cs` (21) وفي مدقّقات `Features/Attachments` (`ChangeNotesTooLong`).
- **البوابات:** `npm run lint` ✅، `npm run build` ✅، `dotnet build` ✅ (0 أخطاء)،
  `dotnet test` ✅ (487/487). أُوقفت جلسة تنقيح Visual Studio التي كانت تقفل
  `Server\bin\...\Infrastructure.dll` (بموافقة المستخدم) قبل البوابات الخلفية، وأُعيد تشغيل خادم
  التطوير على المنفذين 5028/7126 بعدها.

### Phase 1 — أنابيب الرفع والتنزيل (shared/api + shared/lib)

- **المخرجات:**
  - `shared/api/httpClient.ts`: `uploadForm<T>(url, form, { onProgress })` يبني `FormData` ولا يعيّن
    `Content-Type` يدويًا (المتصفح يضع الحدّ الفاصل)، و`downloadBlob(url, { params })` بـ
    `responseType: 'blob'` مع إعادة الترويسات (لا `data` وحدها) لأن اسم الملف منها.
  - تطبيع خطأ التنزيل: إن كان الـ `blob` من نوع `application/json` (أو `application/problem+json`)
    يُقرأ نصّه ويُمرّر في المسار نفسه الذي يبني `ApiError`، فتبقى الرسائل عربية بدل الرسالة العامة.
  - `shared/lib/download.ts`: `parseContentDisposition(header)` يدعم `filename*=UTF-8''` ثم يسقط إلى
    `filename=` ثم إلى الاسم الاحتياطي، و`saveBlobAsFile(blob, headerName, fallbackName)` عبر
    `URL.createObjectURL` + `revokeObjectURL` بعد التنزيل.
  - `shared/lib/crypto.ts`: `sha256Hex(buffer)` → 64 حرفًا سدسيًا صغيرًا، مطابقًا لـ
    `LocalFileStorageService.ComputeSha256Async`، مع معالجة غياب `crypto.subtle` (سياق غير آمن)
    برسالة عربية واضحة لا بانهيار صامت.
  - تحديث `shared/api/README.md` و`shared/lib/README.md` (قلب حالة `crypto.ts` وإضافة `download.ts`).
- **قبول:** البوابات خضراء؛ لا واجهة بعد. تُختبر الأنابيب فعليًا في Phase 2.
- **لمس الخلفية:** لا.
- **الحالة:** ✅ منفذة 2026-11-01.
  - `shared/api/problemDetails.ts`: `toApiErrorAsync(error)` يقرأ جسم `ProblemDetails` الذي يبقى
    `Blob` غير مقروء في طلب `responseType: 'blob'` (يتجاهل الجسم غير الـ JSON ويعيد `null` عند فشل
    التحليل)، و`shared/api/httpClient.ts` صار معترض أخطائه `async` وينتظره قبل فحص 401 والرفض.
  - `shared/api/httpClient.ts`: `uploadForm<T>(url, form, { onProgress })` بلا تعيين `Content-Type`
    وبنسب صحيحة (0-100) يتخطى أحداث التقدم بلا `total`، و`downloadBlob(url, params?)` يعيد
    `{ blob, contentDisposition }`.
  - `shared/lib/crypto.ts`: `sha256Hex(Blob | ArrayBuffer)` → 64 حرفًا سدسيًا صغيرًا، مع
    `ChecksumUnsupportedError` (رسالة عربية) عند غياب `crypto.subtle` بدل الانهيار الصامت.
  - `shared/lib/download.ts`: `parseContentDisposition` و`saveBlobAsFile`، مع تجريدهما مقاطع المسار
    (`/` و`\`) من الاسم لأن القيمة من الخادم ولا يجوز أن توجّه مكان الحفظ على القرص.
  - تحديث مُصدِّرات `shared/api/index.ts` و`shared/lib/index.ts` وREADME-ي المجلدين.
- **انحراف عن الخطة:**
  - `saveBlobAsFile(blob, fileName)` بدل `(blob, headerName, fallbackName)`، و`parseContentDisposition`
    يعيد `null` عند غياب الاسم بدل السقوط إلى اسم احتياطي. السبب: أولوية الاسم (الترويسة ثم
    `originalFileName` ثم معرّف الإصدار) قرار يحتاج بيانات الكيان الذي لا تراه هذه الشريحة، فبقي
    التركيب عند المستدعي في Phase 2 وبقيت هنا دوال نقية قابلة للاختبار.
  - `uploadForm` يستقبل `FormData` مبنيًّا ولا يبنيه: أسماء الحقول (`file`, `fileTypeId`,
    `isPrimaryScan`, …) من شأن `features/attachment-upload` في Phase 3.
  - تطبيع خطأ الـ `Blob` نُفّذ كـ `toApiErrorAsync` في `problemDetails.ts` يستدعيه المعترض، لا كمنطق
    داخل `httpClient.ts` وحده، حتى يبقى بناء `ApiError` في ملف واحد ويصلح لمن يصطاد خطأ axios خارج
    المعترض.
  - توقيع `downloadBlob(url, params?)` بدل `(url, { params })`.
- **قيود التحقق:** لا واجهة بعد — تبويب «الملفات» ما يزال `available: false` — فالتحقق كان بمقارنة
  الخوارزميات بمصدر حقيقة مستقل في Node: مخرجات `sha256Hex` مطابقة لـ `createHash('sha256')` بما
  فيها المدخل الفارغ (`e3b0c442…`)، ومسارَا `Blob` و`ArrayBuffer` متطابقان؛ و`parseContentDisposition`
  اجتاز الحالات السبع عشرة: `filename*=UTF-8''` عربي يُقدَّم على `filename=` لاتيني، اسم مقتبس وغير
  مقتبس، `inline`، مسافات، تهريب مئوي معطوب (يسقط إلى الاسم العادي)، ترويسة غائبة أو بلا اسم أو
  فارغة، وتجريد مقاطع المسار.
- **البوابات:** `npm run lint` ✅، `npm run build` ✅، `dotnet build` ✅ (0 أخطاء)،
  `dotnet test` ✅ (487/487). أُوقف خادم التطوير على 5028 قبل البوابات الخلفية وأُعيد تشغيله بعدها؛
  هذه المرة كان الخادم الذي بدأته بنفسي (سلسلة `bash` ← `dotnet`، لا `VsDebugConsole`/`devenv`) فلم
  تلزم موافقة المستخدم على إيقافه.

### Phase 2 — القائمة والتنزيل في التبويب (widget + تركيب الصفحة)

- **المخرجات:**
  - `widgets/document-attachments-panel/ui/DocumentAttachmentsPanel.tsx`: شبكة `DataTable` بوضع
    `client` و`tableId="document-files"`، أعمدة: العنوان، النوع (عربي عبر `LookupLabel`/المرآة)،
    الإصدار الحالي، الحجم (`formatFileSize` من `versions[0].fileSizeBytes`)، الاسم الأصلي (LTR مع
    `title` كامل)، البصمة المختصرة (`formatShortId`)، «مسح ضوئي أساسي»، الحالة (نشط/مؤرشف)،
    تاريخ الإنشاء وآخر تحديث؛ العمود الاختياري مخفي افتراضيًا حيث يلزم.
  - مرشحات في الرابط عبر `useTableState`: `status` (الكل/نشط/مؤرشف) و`fileType`، مع زر «مسح البحث
    والمرشحات» (`clearQuery()`)، وبحث محلي في العنوان والاسم الأصلي.
  - قائمة سياق للصف (تُوسّع في Phase 3/4): «تنزيل الإصدار الحالي»، «الإصدارات»، «التحقق من البصمة»،
    «نسخ المعرّف»، «نسخ البصمة»؛ والتنزيل متاح لحامل `DOCUMENT_VIEW` فقط.
  - `pages/documents/ui/DocumentFilesTab.tsx` + تفعيل التبويب في `DocumentDetailsPage.tsx`:
    `available: true` (سطر 36)، إضافة `'files'` إلى `AvailableTabKey` (سطر 41)، فرع في `resolveTab`
    (سطور 44-54)، وتركيب مشروط في كتلة التبويبات، مع حالة فارغة عربية («لا ملفات مرفقة بعد»)
    وبلا إجراء رفع (يأتي في Phase 3).
  - تحديث `widgets/document-attachments-panel/README.md` و`pages/documents/README.md`.
- **قبول (متصفح):** `/documents/{id}?tab=files` يعرض ملفات الوثيقة بما فيها المؤرشفة؛ التنزيل يحفظ
  الملف باسمه الأصلي **العربي** صحيحًا ويفتح؛ المرشحات والفرز والبحث تعمل وحالتها في الرابط؛
  وثيقة بلا ملفات تعرض الحالة الفارغة؛ تبديل التبويب لا يكسر بقية التبويبات.
- **لمس الخلفية:** لا.
- **الحالة:** ✅ منفذة 2026-11-01 — **بلا تحقق في المتصفح بطلب المستخدم.**
  - `entities/document-file/api/documentFileApi.ts`: إضافة `download(fileId, versionId?)` فوق
    `downloadBlob` من `shared/api`؛ يعيد `{ blob, contentDisposition }` ومعرّف الإصدار يُرسل فقط عند
    تحديده. التوثيق يثبّت أن مستوى DOWNLOAD لكل وثيقة لا يُعاد إلى العميل أبدًا، لذا `403` هنا إنكار
    حقيقي يُترجم لا شرط يُمنع به الإجراء.
  - **جديدة** `widgets/document-attachments-panel/ui/DocumentAttachmentsPanel.tsx` + `index.ts`:
    شبكة `DataTable` `mode="client"` و`tableId="document-files"`، عشرة أعمدة (العنوان غير قابل للإخفاء،
    النوع عبر `LookupLabel`/المرآة، الإصدار الحالي `v{n}`، الحجم، الاسم الأصلي `dir="ltr"`، «مسح ضوئي
    أساسي»، الحالة `FileArchivedBadge`، ثم البصمة المختصرة و`createdAt` و`updatedAt` مخفيّة افتراضيًا)؛
    مرشّحا `status` و`fileType` في رابط الصفحة عبر `useTableState({ filterKeys: ['status', 'fileType'] })`
    وزر «مسح البحث والمرشحات» (`clearQuery()` بكتابة رابط واحدة)؛ قائمة سياق صف: تنزيل الإصدار الحالي،
    نسخ معرّف الملف/البصمة/الاسم الأصلي؛ و«تحديث الملفات» في `headerMenu` و`emptyMenu`.
  - **جديدة** `pages/documents/ui/DocumentFilesTab.tsx` وتفعيل التبويب في `DocumentDetailsPage.tsx`:
    `available: true`، `'files'` إلى `AvailableTabKey`، فرع `if (raw === 'files')` في `resolveTab`،
    وتركيب مشروط قبل كتلة الصلاحيات. الحالة الفارغة العربية «لا ملفات مرفقة بعد.» بلا إجراء رفع.
  - أولوية اسم الملف عند التنزيل: ترويسة `Content-Disposition` ثم `originalFileName` لمصدر الإصدار ثم
    `file.title` — الترويسة تفوز لأنها تسمي البايتات المُرجَعة فعلًا وصيغتها `filename*=UTF-8''` وحدها
    تحمل الاسم العربي سليمًا.
  - لا فرز افتراضي (`defaultSortKey` مسكوت) احترامًا لترتيب الخلفية الموثّق (الملفات الأقدم فالأحدث).
  - ثمانية README-ات حُدِّثت بدفعات 2026-11-01: `widgets/document-attachments-panel/` (إعادة كتابة)،
    `widgets/`، `entities/document-file/`، `entities/`، `entities/lookup/`، `pages/documents/`،
    `app/router/`، `app/` — صوّب الأخيران سطرًا قديمًا كان يستثني تبويب الملفات من خريطة المسارات.
- **انحراف عن الخطة:**
  - «الإصدارات» و«التحقق من البصمة» مؤجّلتان إلى Phase 4: لا وجود لحوارَيهما بعد، وتركيبهما في قائمة
    السياق الآن يعني ملفًا واحدًا يُعدَّل مرتين. أُضيف بدلًا منهما «نسخ الاسم الأصلي» لأنه متاح الآن.
  - الحجم يُقرأ من الإصدار الحالي (`versions.find(v => v.isCurrent) ?? versions[0]`) لا من
    `versions[0]` حرفيًا كما كتبت الخطة: الترتيب الموثّق يجعلهما متطابقين اليوم، لكن السقوط الصريح
    إلى `versions[0]` أوضح عند أرشفة إصدار قديم.
  - التنزيل معروض لكل حامل `DOCUMENT_VIEW` بلا بوابة صلاحيات إضافية (الخطة تركتها مفتوحة): مستوى
    DOWNLOAD لكل وثيقة غير مرئي للواجهة، فالمحاولة هي المسار الوحيد، ورسالتها العربية تظهر عند الرفض.
- **قيود التحقق:** تحقّق المتصفح أُلغي بطلب المستخدم، لذا معايير القبول — شبكة بملفات حقيقية، اسم تنزيل
  عربي، المرشحات في الرابط، الحالة الفارغة، سلامة بقية التبويبات — **غير موثّقة بصريًا**. ولم تُكتب
  أي بيانات تجريبية في قاعدة التطوير (خيارا «تخطّي المتصفح» و«رفع عبر جلسة الصفحة» كانا متعارضين).
- **البوابات:** `npm run lint` ✅، `npm run build` ✅ (1.25s)، `dotnet build` ✅ (0 أخطاء)،
  `dotnet test` ✅ (487/487). منفذ 5028 أقفل `Server\bin` مرة أخرى قبل البوابات الخلفية؛ الخادم كان
  من تشغيلي (سلسلة `bash` ← `dotnet`، لا `VsDebugConsole`/`devenv`) فأُوقف وأُعيد تشغيله بعدها بلا
  حاجة إلى موافقة.

### Phase 3 — الرفع الذرّي (features/attachment-upload)

- **المخرجات:**
  - `features/attachment-upload/api/uploadInitial.ts`: `POST /api/documents/{documentId}/files`
    بحقول `file`، `fileTypeId`، `title`، `isPrimaryScan` (صريح)، `changeNotes` (اختياري ≤500).
  - `model/upload.schema.ts`: zod برسائل عربية داخلية — العنوان مطلوب ومقصوص ≤300
    (`Attachments.TitleEmpty`/`TitleTooLong`)، النوع مطلوب (`FileTypeEmpty`)، الملف مطلوب
    (`FileEmpty`)، ثم تحقق مسبق: الامتداد ضمن `DOCUMENT_FILE_TYPE_LIMITS` والحجم ≤ حد النوع
    (وحجم الطلب ≤ `MAX_UPLOAD_BYTES`)، مع ربط كل رفض برمزه الخلفي في الرسالة.
  - `ui/UploadAttachmentDialog.tsx`: اختيار النوع من القاموس الحيّ (`useLookupEntries` + المرآة
    احتياطيًا)، عنوان، حقل ملف بـ `accept` مشتق من امتدادات النوع المختار (يُحدَّث عند تغيير النوع)،
    خانة «مسح ضوئي أساسي»، ملاحظات تغيير اختيارية، **وعند اختيار النوع يظهر حد الحجم والامتدادات
    المسموحة نصًا عربيًا**؛ حساب `sha256Hex` للملف مع مؤشر «جارٍ حساب البصمة»، شريط تقدم الرفع من
    `onUploadProgress`، وبعد النجاح مقارنة البصمة المحلية بـ `versions[0].checksumSha256` وشارة
    ✅ «مطابقة»/⚠️ «غير مطابقة»، ثم `toast.success` بالعربية.
  - `api/attachmentMutations.ts`: `useUploadInitialMutation(documentId)` يُبطل
    `documentFileKeys.byDocument(documentId)` في `onSuccess` (نمط `document-create/api/documentMutations.ts:15-27`).
  - زر «رفع ملف» في شريط أدوات الشبكة + إجراء الحالة الفارغة، ظاهر فقط لحامل `DOCUMENT_EDIT`
    ووثيقة غير ملغاة، ومخفي تمامًا (لا معطّل) عند غياب الصلاحية.
  - تحديث `features/attachment-upload/README.md`.
- **قبول (متصفح):** رفع PDF صغير ينشئ ملفًا بإصدار أول وتظهر البصمة المحلية مطابقة للمخزنة والشبكة
  تتحدّث فورًا؛ ملف بامتداد خارج المسموح أو أكبر من حد نوعه يُرفض عربيًا **قبل الإرسال**؛ ملف
  `.exe` مُعاد تسميته إلى `.pdf` يُرفض برسالة `Attachments.SignatureMismatch` العربية؛ نوع موقوف
  يعيد `Attachments.FileTypeInactive` عربيًا؛ حامل `DOCUMENT_VIEW` فقط لا يرى زر الرفع واستدعاء
  النقطة مباشرة يعيد 403.
- **لمس الخلفية:** لا.
- **الحالة:** ✅ منفذة 2026-11-01 — **بلا تحقّق في المتصفح (بطلب المستخدم بعد أن كان اختير الرفع
  الحقيقي).**
  - `features/attachment-upload/api/uploadInitial.ts`: يبني `FormData` بالحقول الخمسة ويرسله عبر
    `uploadForm<DocumentFileResponse>` من `shared/api` (بلا `Content-Type` يدويًا).
  - `api/attachmentMutations.ts`: `useUploadInitialMutation(documentId)` يُبطل
    `documentFileKeys.byDocument(documentId)` في `onSuccess`، فتتحدّث الشبكة من تلقائها.
  - `model/upload.schema.ts`: `uploadAttachmentSchema` — العنوان مطلوب ≤300، النوع مطلوب، الملف
    مطلوب، ثم فحص مسبق: حجم ≤ `MAX_UPLOAD_BYTES` ثم ≤ `maxBytes` للنوع، والامتداد ضمن
    `allowedExtensions` (بأحرف صغيرة، واسم بلا امتداد مرفوض). كل رسالة تذكر رمزها الخلفي بصيغة
    `(Attachments.*)`.
  - `ui/UploadAttachmentDialog.tsx`: نوع من القاموس الحيّ (`useLookupEntries` + المرآة placeholder)،
    `accept` ونصّ الحدود يتبعان النوع المختار، حقل ملف، خانة «مسح ضوئي أساسي»، ملاحظات تغيير، بصمة
    محلية بـ `sha256Hex` مع مؤشر «جارٍ حساب البصمة» وحارس ضد استبدال سريع للملف، شريط تقدّم
    `role="progressbar"` من `onProgress`، ثم لوحة نتيجة فيها `ChecksumBadge` (مطابقة/غير مطابقة/لم
    تُحسب) بدل إغلاق الحوار تلقائيًا.
  - التركيب في `widgets/document-attachments-panel` (`canUpload`) وتمريره من
    `DocumentDetailsPage` بقيمة `canEdit && !isCanceled`؛ الزر في `toolbar` وفي `emptyAction` فقط حين
    لا ملفات أصلًا. المُصدِّر `index.ts` أُنشئ للشريحة.
  - READMEs المحدّثة: `features/attachment-upload` (من وثائق فقط إلى منفّذ)، `features/README.md`،
    `widgets/document-attachments-panel/README.md`، `widgets/README.md`، `pages/documents/README.md`.
- **انحراف عن الخطة:**
  - فحص «لا ملف» نُقل إلى `superRefine` مع بقية قواعد الملف بدل `refine` على الحقل: نسخة zod 4 من
    `z.file().nullable().refine()` ضيّعت نوع الخرج إلى `File` فأصبح `defaultValues: { file: null }`
    ومستدعي `zodResolver` متعارضين؛ وحده `nullable` بلا `refine` يبقيهما متطابقين.
  - النوع الافتراضي في الحوار «مسح ضوئي» بدل قائمة فارغة، لأن نصّ الحدود و`accept` يحتاجان نوعًا
    مختارًا ليظهرا من أول فتح.
  - رسالة تجاوز حد النوع لا تذكر حجم الملف نفسه: التقريب في `formatFileSize` كان يجعل ملفًا يزيد
    بايتًا واحدًا عن 20 م.ب يقرأ «20 م.ب يتجاوز 20 م.ب».
  - لوحة النتيجة تبقى مفتوحة بعد النجاح (الخطة اكتفت بـ `toast.success`) لأن شارة المطابقة بلا معنى
    في إشعار يختفي.
  - الأخطاء الخلفية لا تُعرض في `toast` بل في لافتة داخل الحوار، حتى تبقى رسالة الرفض بجانب الحقل
    المعني وتتفادى عرض السبب مرتين.
- **قيود التحقق:** تحقّق المتصفح أُلغي بطلب المستخدم، فلم تُختبر فعليًا: مطابقة البصمة بعد رفع حقيقي،
  شريط التقدّم، `Attachments.SignatureMismatch` لملف `.exe` مُعاد تسميته، و`Attachments.FileTypeInactive`
  لنوع موقوف، ورفض زر الرفع لحامل `DOCUMENT_VIEW` وحده. لم تُكتب أي بيانات في قاعدة التطوير.
  البديل: 14 حالة على المخطط الحقيقي في Node (`new File` حقيقي، استيراد الوحدة نفسها عبر خطّاف
  resolver يؤجّل حواجز `import.meta.env`)، نتيجتها 14/14: قبول PDF ضمن الحدود، رفض بلا ملف، رفض ملف
  فارغ، رفض `.exe` مع نوع المسح، رفض اسم بلا امتداد، قبول `Scan.PDF` بأحرف كبيرة، اجتياز `.exe`
  المُعاد تسميته (التوقيع خلفي)، رفض تجاوز حد النوع، رفض تجاوز سقف الطلب، عدم رفض نوع غير معروف
  محليًا، ورسائل العنوان/النوع/الملاحظات الطويلة.
- **البوابات:** `npm run lint` ✅، `npm run build` ✅ (1.45s)، `dotnet build` ✅ (0 أخطاء)،
  `dotnet test` ✅ (487/487). أوقف خادم التطوير على 5028 قبل البوابات الخلفية (من تشغيلي: سلسلة
  `bash` ← `dotnet`) وأُعيد بعده، وأُعيد تشغيل خادم الواجهة أيضًا.

### Phase 4 — الإصدارات والأرشفة والتحقق

- **المخرجات:**
  - `api/uploadVersion.ts` (`POST /api/document-files/{fileId}/versions`: `file` + `changeNotes`)،
    `api/archiveFile.ts` (`POST /api/document-files/{fileId}/archive`)، وتمديد مساعد التنزيل
    بتمرير `versionId` اختياريًا.
  - `ui/UploadVersionDialog.tsx`: ملف + ملاحظات تغيير، يعرض «الإصدار التالي: N+1» من
    `currentVersionNumber`، ويحسب البصمة المحلية ويقارنها، ويرفض عربيًا على ملف مؤرشف قبل الإرسال
    (`Attachments.FileArchived`).
  - `ui/DocumentVersionsDialog.tsx`: جدول الإصدارات تنازليًا — الرقم، الاسم الأصلي، الحجم، البصمة
    المختصرة، شارة «الحالي»/«مُستبدَل»، ملاحظات التغيير، الرافع، التاريخ، وتنزيل لكل إصدار.
  - `ui/ArchiveFileButton.tsx` + `ConfirmDialog` عربي (تحذير: الأرشفة تمنع إصدارات جديدة ولا تحجب
    القراءة/التنزيل)، مع معالجة 409 وكون الأرشفة idempotent في الخلفية.
  - إجراء «التحقق من البصمة» يستدعي `useVerifyChecksum` ويعرض في الحوار: البصمة المسجّلة،
    المحسوبة من القرص، `isMatch`، والبصمة المحلية إن حُسبت — ثلاث مقارنات مسمّاة بوضوح.
  - إكمال قائمة سياق الصف بكل الإجراءات مع إخفاء ما لا صلاحية له.
- **قبول (متصفح):** رفع إصدار ثانٍ يجعل الأول «مُستبدَل» والثاني «الحالي» ويزيد
  `currentVersionNumber`؛ تنزيل إصدار قديم يعمل بـ `versionId`؛ الأرشفة تُظهر الشارة وتمنع إصدارًا
  جديدًا برسالة عربية؛ «التحقق من البصمة» يعيد مطابقة لملف سليم؛ ملف فارغ يُرفض بـ
  `Attachments.FileEmpty`.
- **لمس الخلفية:** لا.
- **الحالة:** ✅ منفذة 2026-11-01 — **بلا تحقّق في المتصفح (بطلب المستخدم).** نُفّذت كل المخرجات
  السبع، مع انحرافات موثَّقة أدناه. البوابات الأربع خضراء بعد التنفيذ:
  `npm run lint` ✅، `npm run build` ✅ (1.40s)، `dotnet build InstitutionalDiwan.slnx` ✅ (0 أخطاء)،
  `dotnet test` ✅ 487/487. أُوقف خادم التطوير على 5028 قبل البوابتين الخلفيتين وأُعيد بعده.
  - **انحراف 1 — `ArchiveFileButton.tsx` صار `ArchiveFileDialog.tsx`:** إجراءات الصف كلها في قائمة
    السياق (بيانات لا مكوّنات زر)، فالأرشفة حوار `ConfirmDialog` مستقل يستقبل الملف ويعالج الفشل
    بإبقاء الحوار مفتوحًا مع رسالة عربية.
  - **انحراف 2 — «الجدول» صار بطاقات:** `DocumentVersionsDialog` قائمة بطاقات لا `DataTable`؛
    الحوار لا يحتاج حالة URL ولا مُنتقي أعمدة، وكل إصدار يحتاج زر تنزيل خاصًا به. الحقول المطلوبة
    كلها موجودة (الرقم، الاسم الأصلي، الحجم، البصمة المختصرة، شارة الحالي/مُستبدَل، ملاحظات
    التغيير، الرافع، التاريخ) والترتيب تنازلي كما تأتي من الخادم.
  - **انحراف 3 — اسمان انتقلا إلى الكيان:** `currentVersionOf(file)` (كان مساعدًا خاصًا بالودجت)
    و`downloadFileVersion(file, version?)` في `entities/document-file`، لأن الحوارات والودجت تحتاج
    نفس قاعدة «الإصدار الحالي» ونفس أولوية اسم الملف المحفوظ؛ استُخدم `versionId` الموجود أصلًا في
    `documentFileApi.download` بدل تمديده (أُنجز في Phase 2).
  - **انحراف 4 — إعادة تسمية `canUpload` إلى `canManage`:** البوابات الثلاث للكتابة (رفع ملف، رفع
    إصدار، أرشفة) تشترط `DOCUMENT_EDIT` على وثيقة غير ملغاة، فالاسم الجديد أدق. التنزيل والإصدارات
    والتحقق تبقى للقارئ.
  - **انحراف 5 — رفض المؤرشف حارس حالة لا مسار أساسي:** قائمة الصف تخفي «رفع إصدار» و«الأرشفة» عن
    صف مؤرشف أصلًا، فبقاء `UploadVersionDialog` على بيانات قديمة هو الطريق الوحيد للرسالة
    العربية `Attachments.FileArchived`؛ تُعرض قبل أي طلب كما اشترطت الخطة. الأرشفة في الخلفية
    idempotent (تعيد نجاحًا لا 409)، و409 يبقى واردًا من أنواع موقوفة (`Attachments.FileTypeInactive`)
    ويُترجم عربيًا في لافتة الحوار.
  - **تجميع مستحدث:** `fileRejection(file, fileTypeId)` صار جدول القواعد الوحيد (فارغ، سقف الطلب،
    الامتداد، حد النوع) ويستدعيه مخطّط الرفع الأول و`buildUploadVersionSchema(fileTypeId)`؛
    `useLocalChecksum` و`FilePickSummary` استُخرجت من حوار الرفع الأول بدل تكرارهما.
  - **حدود التحقق:** فُحصت القواعد في Node مقابل نفس وحدات التنفيذ الحقيقية (21 حالة، كلها ناجحة):
    اثنتا عشرة قاعدة `fileRejection`، أربعة مسارات على المخططين (`safeParse` ومسار الخطأ `file`)،
    وثلاث حالات `currentVersionOf` (الشارة تسبق الترتيب، الاحتفاظ بأحدث، تاريخ فارغ). حُذفت ملفات
    الفحص المؤقتة بعد ذلك. **غير موثَّقة:** رسم الحوارات نفسه، أشرطة التقدّم، التنبيهات، قراءة
    التحقق الحية من الخادم، وخصلة الصف المؤرشف. معيار القبول المتصفح المؤجل (إصدار ثانٍ يجعل
    الأول «مُستبدَل»، تنزيل إصدار قديم، الأرشفة، بصمة ملف سليم، رفض ملف فارغ) يُرحَّل إلى Phase 5.
  - **لمس الخلفية:** لا. لا وثائق بذر ولا مستخدمين أُضيفوا.
  - **وثائق حُدِّثت في نفس المرحلة:** `features/attachment-upload/README.md`،
    `widgets/document-attachments-panel/README.md`، `entities/document-file/README.md`،
    `pages/documents/README.md`، `features/README.md`، `widgets/README.md`.

### Phase 5 — مصفوفة الأدوار والتحقق النهائي والتوثيق

- **المخرجات:**
  - تحقق في المتصفح بثلاثة أوضاع: مدير (كل الإجراءات)، مستخدم `USER` عادي (يملك `DOCUMENT_VIEW` +
    `DOCUMENT_EDIT` فتظهر له الإجراءات لكنها تخضع لمستوى وصوله على الوثيقة)، وقراءة فقط
    (بلا `DOCUMENT_EDIT`: لا أزرار كتابة، والتنزيل والتحقق فقط). يُعاد استخدام `num.viewer` إن كفى،
    وإلا أُنشئ مستخدم تحقق عبر سكربت مؤقت خارج المستودع (بلا طباعة رموز أو أسرار).
  - عيّنة «مستوى وصول أدنى من المطلوب»: مستخدم ممنوح VIEW فقط على وثيقة ثم محاولة تنزيل →
    `Correspondence.DocumentAccessDenied` بالعربية، ومحاولة رفع → الرسالة نفسها (MANAGE مطلوب).
  - مسارات الأخطاء المتبقية: ملف أكبر من سقف الطلب (~51MB)، نوع غير موجود (`FileTypeNotFound`)،
    إصدار غير معروف (`VersionNotFound`)، ملاحظات تغيير >500 (`ChangeNotesTooLong`).
  - **عيّنة تحقق دائمة في قاعدة التطوير:** وثيقة تحمل ملف «مسح ضوئي» بإصدارين (أحدهما مُستبدَل)،
    وملف «توقيع» مؤرشف، وملف «مرفق» بامتداد Office — تُوثَّق معرّفاتها في هذه الخطة كما في
    Phase 5 من خطة الموديول 4.
  - **البوابات الأربع:** `npm run lint`، `npm run build`، `dotnet build`، `dotnet test` (487)، مع
    إيقاف خادم التطوير على المنفذ 5028 قبل البوابات الخلفية وإعادة تشغيله بعدها.
  - تحديث الوثائق في نفس المرحلة: `README.md` الجذر (الحالة العليا، جدول الموديولات — قلب الموديول 6
    إلى «Implemented and verified»، فقرة ما بعد الجدول، وسجل التغيير)، `institutionaldiwan.front/README.md`،
    `institutionaldiwan.front/knowledge.md`، وREADMEs الشرائح الملموسة كلها (`entities/document-file`،
    `features/attachment-upload`، `widgets/document-attachments-panel`، `pages/documents`،
    `shared/api`، `shared/lib`، `shared/config`، `entities/lookup`، `features/README.md`، `widgets/README.md`).
  - قلب حالة هذه الخطة إلى «✅ مكتملة (التاريخ)» مع تسجيل نتائج التحقق والانحرافات والقيود.
- **لمس الخلفية:** لا (إنشاء مستخدم/منح التحقق في قاعدة التطوير فقط).
- **الحالة:** ⏳ لم تُنفّذ.

## 5. خارج النطاق صراحةً

- أي نقطة نهاية جديدة أو أي تغيير في الخلفية — بما فيه كشف `allowedExtensionsCsv`/`maxFileSizeBytes`
  في استجابة القاموس، وكشف مستوى وصول المستخدم الحالي للوثيقة. كلاهما يُسجَّل كتحسين خلفي مقترح.
- تقسية `DocumentFileResponse.relativePath` (ثغرة 10): الواجهة لا تعرضه، والخلفية لا تُمس.
- إدارة قاموس أنواع الملفات (تحتاج `LOOKUP_MANAGE`؛ إدارة القواميس العامة غير منفذة أصلًا — قيد
  الموديول 1 الموثّق في جدول الحالة).
- معاينة محتوى الملف داخل المتصفح (عارض PDF/صور) — تنزيل فقط.
- رفع جماعي أو سحب وإفلات متعدد الملفات (الخادم يستقبل ملفًا واحدًا لكل نداء).
- استئناف الرفع المقطوع، رغم دعم الخادم معالجة النطاقات في التنزيل.
- حذف ملف أو إصدار، أو تعديل عنوان ملف بعد إنشائه (لا نقطة نهاية لذلك).
- الأرشفة (07)، العلاقات (08)، الإشعارات (10)، الإحصاءات.

## 6. المخاطر والقرارات

| الخطر/القرار | المعالجة المقترحة |
| --- | --- |
| انحراف مرآة الحدود عن بيانات البذر إذا تغيّرت | تعليق في `shared/config/attachmentLimits.ts` يشير إلى `AttachmentsLookupSeeders.cs:27-45`، وسلوك احتياطي «أشد الحدود» للنوع غير المعروف، والخادم يبقى الحكم ورسائله عربية |
| أسماء ملفات عربية في `Content-Disposition` | دعم `filename*=UTF-8''` أولًا ثم `filename=` ثم اسم احتياطي من العنوان + الامتداد |
| خطأ التنزيل يعود `blob` فلا يُطبَّع | كشف `application/json` في الـ blob وقراءة نصّه وتمريره في `toApiError` (ثغرة 4) |
| حساب بصمة ملف 50MB في الذاكرة | `arrayBuffer()` لمرة واحدة + تعطيل الزر أثناء الحساب + مؤشر «جارٍ حساب البصمة»؛ يُوثَّق القيد في README الشريحة |
| غياب `crypto.subtle` خارج HTTPS | رسالة عربية واضحة بدل بصمة خاطئة صامتة؛ سياق التطوير عبر الوكيل يبقى HTTPS |
| مستوى وصول الوثيقة غير معروف مسبقًا (ثغرة 7) | لا تعطيل احترازي للأزرار؛ الصلاحية تُخفيها، و403/404 يُترجمان عربيًا عند الوقوع |
| `isPrimaryScan` غير قابل للفراغ في الخادم | إرسال `false` صراحةً دائمًا في نموذج الرفع |
| القائمة تشمل المؤرشف ولا ترقيم فيها | وضع `client` + مرشح حالة افتراضي «الكل» وشارة «مؤرشف» واضحة |
| سقف الطلب ~51MB مقابل حدود الأنواع | تحقق مسبق مزدوج: حد النوع ثم سقف الطلب، برسالتين عربيتين مختلفتين |
| `Attachments.ChangeNotesTooLong` غير معرّف في `DomainErrors.cs` | يُضاف إلى الكتالوج العربي anyway لأن `ValidationPipelineBehavior` يرفع نصّه كـ `title` |
| لا فحص فيروسات في الخادم (توقيع بايتات أول 16 فقط) | يُذكر في README الودجت كتوثيق للحدود؛ لا تغيير خلفي |
| تبديل التبويب يصفّر استعلام التبويب السابق | سلوك مقصود ومطابق لبقية تبويبات `/documents`؛ يُوثّق في README الصفحة |

## 7. تسلسل التنفيذ

```
Phase 0 → Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5
```

لا تغيير خلفي في أي مرحلة؛ البوابات الخلفية تُشغّل فقط للتأكد من عدم الانحدار.
الالتزام بمراجعة بعد كل مرحلة قبل بدء التالية، والتحقق في المتصفح إلزامي من Phase 2.
**استثناء موثَّق:** تحقّق المتصفح لـ Phase 2 (تبويب الملفات) ولـ Phase 3 (الرفع الذرّي) ولـ Phase 4
(الإصدارات والأرشفة والتحقق) أُلغي بطلب المستخدم صراحةً في 2026-11-01، فمعايير قبول التبويب كلها غير
موثَّقة بصريًا وتُرحَّل إلى أول جلسة تحقق لاحقة. البديل headless: قواعد الفحص المسبق فُحصت في Node مقابل
نفس وحدات التنفيذ الحقيقية (14 حالة في Phase 3، 21 في Phase 4 شملت مخطّط الإصدار و`currentVersionOf`)؛
مسارات بصمة ما بعد الرفع وشريط التقدّم و`SignatureMismatch` و`FileTypeInactive` وإخفاء الأزرار عن القارئ
وإجراءات الصف الجديدة كلها غير موثَّقة.
