# خطة الموديول 11 — الملفات (Drive، واجهة SPA)

> **الحالة:** Phases 0–1 منفّذتان؛ تنفيذ متواصل للمراحل 2–6 بطلب المستخدم («كمل للآخر وبعدين بشوفهم
> دفعة وحدة») ثم مراجعة واحدة شاملة. البوابات والوثائق تُحدّث مع كل مرحلة؛ لا commits إلا بطلب صريح.
> **النطاق:** إغلاق واجهة `institutionaldiwan.front` للموديول 11 («الملفات»، تجربة Google Drive)
> وفق §9.2 من خطة الخلفية `module-11-drive-backend.md`: شريط جانبي (ملفاتي/المشتركة/المفضلة/السلة)،
> شبكة/قائمة، breadcrumb، سحب وإفلات، معاينة، محرر مضمّن، لوحة إصدارات، حوار صلاحيات، عداد حصة —
> **بلا أي تغيير خلفي**.
> **الخلفية:** خلفية الموديول 11 مكتملة ومغلقة (Phases 0–5، commit `ff04421`، ‏1264 اختبارًا أخضر،
> صفر أخطاء بناء): 41 نقطة نهاية تحت `/api/drive/*`، رفع presigned مباشر من المتصفح إلى SeaweedFS،
> تنزيل/Range، إصدارات غير قابلة للتغيير + استرجاع بالنسخ، صلاحيات هرمية (Viewer/Editor/Manager)،
> حذف بموافقة رئيس القسم، سلة 30 يومًا، حصص لكل مساحة، وتحرير Word/Excel/PowerPoint مضمّن عبر
> OnlyOffice. كتالوج `labels/drive.ts` مزروع مسبقًا (56 مفتاح `drive.*`)، ومرآة `permissions.ts`
> تحوي `DRIVE_USE`/`DRIVE_MANAGE`. **بعد Phase 0:** توسّع `drive.ts` إلى 185 مفتاحًا (+129 نسخ شاشات)،
> وأُضيفت رموز `error.drive.*` الـ64 إلى `errors.ts`، و`nav.drive`/`page.drive.*` إلى `nav.ts`/`pages.ts`.
> **آخر مراجعة:** 2026-11-20.

---

## 1. الوضع الحالي المؤكد (تم فحصه)

| العنصر | الحالة |
| --- | --- |
| نقاط النهاية الخلفية `/api/drive/*` (41 نقطة، 8 متحكّمات) | ✅ جاهزة ومختبرة (`Server/Controllers/Drive*Controller.cs`)؛ الواجهة ❌ لا تستهلك أيًّا منها |
| الرفع المباشر presigned PUT إلى SeaweedFS (`initiate`→PUT→`complete`/`abandon`) | الخلفية جاهزة؛ الواجهة ❌ (لا مساعد PUT خارجي في `httpClient.ts`) |
| التنزيل/المعاينة (`content` يدعم Range يدويًا 206، `download-url` presigned ‏5 دقائق) | الخلفية جاهزة؛ الواجهة ❌ |
| الإصدارات (`versions`، `versions/{id}/content`، `restore` بالنسخ) | الخلفية جاهزة؛ الواجهة ❌ |
| الصلاحيات (`permissions` منح/سحب/قائمة، `effective` يعيد `roleId` ‏0 عند انعدام الوصول، `inheritance`) | الخلفية جاهزة؛ الواجهة ❌ |
| الحذف (`deletion-requests` طلب/موافقة/رفض/إلغاء، `DELETE items/{id}` مباشر، `trash` قائمة/استعادة/نهائي) | الخلفية جاهزة؛ الواجهة ❌ |
| المساحات (`spaces` قائمة/إنشاء/شخصية/قراءة/تسمية/حصة) والإعدادات (`settings` get/put بـ `DRIVE_MANAGE`) | الخلفية جاهزة؛ الواجهة ❌ |
| OnlyOffice (`files/{id}/edit-config` يعيد config موقّعًا كاملًا، `onlyoffice/callback` بـ JWT) | الخلفية جاهزة؛ الواجهة ❌ (لا محمّل DocsAPI) |
| مرآة الصلاحيات `shared/config/permissions.ts` | ✅ `DriveUse: 'DRIVE_USE'` و`DriveManage: 'DRIVE_MANAGE'` موجودتان (`:32-35`) |
| كتالوج `shared/config/labels/drive.ts` | ✅ 185 مفتاح `drive.*` (56 عقدًا + 129 نسخ شاشات في Phase 0) مبثوث في `index.ts`؛ تستهلك الشارات/`QuotaMeter` مفاتيح التعداد/الدور/الحصة الآن، والباقي تحذيرات orphan متوقعة حتى تُركّب في مراحلها |
| `shared/api/errorMessages.ts` + `labels/errors.ts` | ✅ Phase 0: كل رموز `Drive.*` الـ64 → مفاتيح `error.drive.*` عربية |
| `labels/nav.ts` / `labels/pages.ts` / `shell-nav.ts` / `routes.ts` / `AppRouter.tsx` | ❌ لا مدخل «الملفات» ولا مسار `/drive` |
| عميل HTTP `shared/api/httpClient.ts` | ✅ axios + معترضات (Bearer، تطبيع ProblemDetails، ‏401→login) + `uploadForm` (multipart) + `downloadBlob` (blob + تطبيع الخطأ) + `putPresigned` (PUT خارجي presigned إلى SeaweedFS، axios مجرّد، أُضيف في Phase 0) |
| البصمة المحلية `shared/lib/crypto.ts` + `useLocalChecksum` | ✅ `sha256Hex(Blob)` عبر WebCrypto + خطاف بحساب غير متزامن؛ قابل لإعادة الاستخدام في `complete` |
| حدود المرفقات `shared/config/attachmentLimits.ts` + `upload.schema.ts` | ✅ نمط مرآة حدود + تحقق مسبق zod برسائل عربية و`withCode`؛ يُنسخ الشكل لـ Drive |
| الشبكة `shared/ui/data-table.tsx` + `useTableState` | ✅ وضع server/client، بحث/فرز/ترقيم/مرشحات في الرابط، منتقي أعمدة، قوائم سياق، تصدير xlsx؛ `MAX_PAGE_SIZE=100` |
| `ConfirmDialog` / `Dialog` / `PageHeader` / `FormField` / `RequirePermission` / `GuideProvider` | ✅ جاهزة لإعادة الاستخدام |
| أدلة driver.js (`shared/config/guides/*`) + حارسا lint | ✅ نمط دليل لكل صفحة + حارس تطابق ثنائي الاتجاه؛ ❌ لا دليل Drive |
| `knowledge.md` | ❌ سطر «Module 11, API pending» قديم (`:135`)؛ يجب تحديثه بالنقاط الحيّة (قاعدة AGENTS.md) |
| لا مشغّل اختبارات أمامي | الحارسان `check-label-keys.mjs` و`check-guide-ids.mjs` يعملان ضمن `npm run lint` |

## 2. الفجوات المطلوب سدّها (أساس الخطة)

| # | الفجوة | الدليل |
| --- | --- | --- |
| 1 | لا شرائح أمامية للموديول 11 إطلاقًا (entities/features/widgets/pages) | `src/entities/`، `src/features/`، `src/widgets/`، `src/pages/` |
| 2 | لا مسار `/drive` ولا مدخل شريط جانبي «الملفات» | `routes.ts`، `AppRouter.tsx`، `widgets/app-shell/model/shell-nav.ts` |
| 3 | لا رموز عربية لأي من رموز `Drive.*` الاثنين والخمسين | `DomainErrors.cs:468-546` مقابل `errorMessages.ts` |
| 4 | لا مساعد PUT presigned خارجي (البايتات تذهب إلى SeaweedFS لا إلى خادمنا، بلا JWT ولا baseURL) | `httpClient.ts` (كل النداءات عبر المثيل المعترَض) |
| 5 | لا مرآة لحدود Drive (الحصة الافتراضية/حد الملف/الامتدادات) — الإعدادات تُقرأ بـ `DRIVE_MANAGE` فقط فلا تتاح للمستخدم العادي | `DriveSettingsController` (`[Authorize DriveManage]`) |
| 6 | لا مرآة لثوابت Drive البنيوية (kind/role/principal/version-status/source/deletion-status/space-type) | `DriveCatalog.cs` |
| 7 | لا محمّل OnlyOffice DocsAPI ولا مكوّن محرر مضمّن | لا وجود لـ `DocsAPI` في الواجهة |
| 8 | لا معاينة ملفات (صور/PDF/فيديو/Office) في الواجهة | الموديول 06 تنزيل فقط |
| 9 | لا سحب وإفلات (رفع بالإفلات، ونقل عنصر إلى مجلد) | لا مكتبة DnD في `package.json` |
| 10 | `knowledge.md` قديم («API pending») و`AGENTS.md` يذكر «17 صلاحية» بينما الفعلية 25 | `knowledge.md:135`، `AGENTS.md` |
| 11 | لا دليل تعليمي لصفحة Drive | `shared/config/guides/` |

> ملاحظة عقدية: الخلفية **مغلقة**؛ أي فجوة عقدية (مثل عدم إتاحة الإعدادات للقراءة العادية) تُعالج
> بمرآة ثابتة في الواجهة وتُسجَّل كتحسين خلفي مقترح، **دون مسّ الخلفية**.

## 3. قرارات معمارية

1. **FSD الصارم كما هو معتمد:** `app → pages → widgets → features → entities → shared`، كل شريحة
   تُصدّر واجهتها من `index.ts` (لا استيراد عميق)، والقراءات في `entities` والكتابات في `features`،
   وكل كتابة تُبطل مفاتيح استعلام الكيان عبر مصنّع المفاتيح لا نصًّا يدويًا. README لكل مجلد ملامس
   (Contents + Implementation status + Change Log) يتحدّث مع الكود في **نفس المرحلة**، و`knowledge.md`
   يُحدَّث عند هبوط النقاط.
2. **لا تغيير خلفي.** كل العقد مأخوذ حرفيًا من المتحكّمات/ـDTOs (§5). مرآة الحدود وثوابت Drive في
   `shared/config` مطابقة للبذر/`DriveCatalog.cs`، والخادم يبقى الحكم وأخطاؤه تُعرض بالعربية.
3. **الرفع presigned مباشر (ثلاث خطوات):** `initiate` (حجز إصدار معلق + فحص مسبق) → **PUT خارجي**
   إلى `uploadUrl` عبر axios مجرّد (بلا معترض Bearer ولا baseURL) مع `onUploadProgress` → `complete`
   (يرسل `checksumSha256` المحلي الاختياري) أو `abandon` عند الإلغاء/الفشل. البايتات لا تمرّ عبر
   `/api/drive` إطلاقًا. رؤوس PUT (خاصة `Content-Type`) يجب أن تطابق ما وقّعه الخادم — يُتحقق في
   Phase 3 مقابل `S3DriveStorageService` (نقطة مفتوحة §9).
4. **التنزيل/المعاينة مساران:** (أ) `download-url` presigned ‏(5 دقائق) للتنزيل المباشر ولـ`<video>`/
   الوسائط التي لا تستطيع إرفاق JWT؛ (ب) `content` المعترَض (Bearer) عبر `downloadBlob` لمعاينة
   الصور/PDF كـ object-URL وللتنزيل داخل التطبيق مع دعم Range. عند انتهاء صلاحية الرابط يُعاد جلبه.
   أسماء الملفات العربية تُقرأ من `Content-Disposition` بـ `filename*=UTF-8''` أولًا (المساعد موجود).
5. **بوابة الصلاحية مزدوجة:** `DRIVE_USE` يخفي/يظهر المسار والمدخل والأزرار العامة (عبر
   `RequirePermission` + `hasPermission`)، و`DRIVE_MANAGE` للإعدادات وتجاوز حصة المساحة والحذف
   النهائي حيث يسري. أما إجراءات العنصر الواحد فتُبوَّب بـ**الدور الفعلي** من
   `GET items/{id}/effective` (`roleId` ‏0/1/2/3): Viewer تصفح/معاينة/تنزيل، Editor +رفع/تحرير/تسمية/
   نقل/مجلدات/استرجاع إصدار، Manager +صلاحيات/كسر وراثة/نقل بين المساحات. على عكس المرفقات (حيث كان
   مستوى الوصول مجهولًا)، هنا **يمكن تعطيل الأزرار بدقة** اعتمادًا على `effective`.
6. **الشبكة وضع server للتصفح:** `GET items` يعيد `PagedResult` (page/pageSize/totalCount/totalPages/
   hasPrevious/hasNext) ⇒ `DataTable mode="server"` + `useTableState` بمرشحات في الرابط
   (`spaceId,parentId,kind,ownerUserId,trashed,starred,modifiedFrom,modifiedTo`) وبحث وفرز
   (`name|modifiedAt|size` + `sortDescending`). القوائم المصفوفة (`children`/`versions`/`permissions`/
   `breadcrumb`) وضع client. السلة وطلبات الحذف server (مصَفّحة). حجم الصفحة الافتراضي 10 (عرف الشبكة
   القائم)، والخادم ي clamp ‏1..100.
7. **صفحة واحدة `/drive` بأوضاع في الرابط:** `view=my-files|shared|starred|trash|deletion` +
   `spaceId` + `parentId` + مرشحات الشبكة، كلها `useSearchParams` (قابلة للحفظ/المشاركة). الشريط
   الجانبي يبدّل الوضع/المساحة، وbreadcrumb يبدّل `parentId`. لا مسارات تفاصيل منفصلة في المرحلة
   الأولى؛ المعاينة/الإصدارات/الصلاحيات حوارات فوق الشبكة.
8. **السحب والإفلات أصلي (HTML5) + بديل لوحة مفاتيح:** إفلات ملفات على المتصفح ⇒ رفع إلى المجلد
   الحالي؛ سحب صف إلى مجلد ⇒ `move`. لا مكتبة خارجية (غير موجودة في الحزمة). دائمًا حوار «نقل»
   متاح للوحة المفاتيح/اللمس وقارئ الشاشة (عقد إمكانية الوصول). الاتجاه المنطقي RTL/LTR.
9. **المحرر المضمّن OnlyOffice:** تحميل كسول لـ`{serverUrl}/web-apps/apps/api/documents/api.js` ثم
   `new DocsAPI.DocEditor(placeholder, config)` حيث `config` **كما ورد حرفيًا** من `edit-config`
   (يتضمن `token` الموقّع و`document.key` و`callbackUrl`). الوضع edit/view حسب الدور والامتداد.
   `destroy()` عند الإغلاق. فشل الحفظ يصل كإشعار (النوع 17) في صندوق الإشعارات + toast من أحداث
   المحرر؛ لا تستدعي الواجهة `callback` إطلاقًا (خادم OnlyOffice يستدعيه). التحرير الحيّ يحتاج ضبط
   OnlyOffice + SeaweedFS (اعتمادية §8).
10. **المعاينة:** صور/PDF عبر object-URL من `content`؛ فيديو عبر `download-url` presigned (Range من
    SeaweedFS)؛ Office عبر عارض OnlyOffice (edit-config بوضع view) حين يكون مضبوطًا، وإلا تنزيل.
    الامتدادات غير المدعومة ⇒ تنزيل فقط برسالة عربية.
11. **عداد الحصة:** من `DriveSpaceResponse.usedBytes/effectiveQuotaBytes` ⇒ مقياس استخدام في الشريط
    الجانبي/رأس المساحة؛ **السلة تُحتسب ضمن الحصة** حتى الحذف النهائي (سلوك Drive). تنبيه اقتراب
    الامتلاء + منع الرفع مسبقًا عند تجاوز الحصة/حد الملف (مرآة §5) والخادم يحكم نهائيًا.
12. **التحقق المسبق zod:** اسم العنصر ≤255 ومنع `\ / : * ? " < > |` و`.`/`..`، اسم المساحة ≤150،
    سبب الحذف ≤500، ملاحظة المراجعة ≤500، الامتداد ضمن المسموح، الحجم ≤ حد الملف، الحصة المتبقية —
    برسائل عربية داخل المخطط و`withCode(msg, 'Drive.*')` على غرار `upload.schema.ts`.
13. **الثوابت البنيوية مرآة لا lookups:** kind/role/principal/version-status/source/deletion-status/
    space-type/session-status ثوابت في `shared/config/lookupIds.ts` (أو `driveIds.ts`) مطابقة
    لـ`DriveCatalog.cs`، والترجمة عبر `drive.*` الموجودة. لا تُجلب من قاموس قابل للتحرير.
14. **الأدلة التعليمية:** دليل `guides/drive.ts` (مناطق `drive.sidebar`/`drive.grid`/`drive.breadcrumb`/
    `drive.upload`/`drive.versions`/`drive.permissions`/`drive.quota` + أدوات الشبكة المشتركة
    `table.*`)، يُسجَّل في `guides/index.ts`، وحارس lint يفرض التطابق الثنائي.
15. **حالة كل شاشة إلزامية:** تحميل skeleton، فارغ عربي، خطأ + إعادة محاولة، ممنوع مخصّص، نجاح
    toast + إبطال مفاتيح. قوائم سياق على كل سطح بيانات (صف/رأس/فارغ) مفلترة بالصلاحية/Dور وقابلة
    للوحة المفاتيح (`Shift+F10`). لا hex في المكوّنات، CSS منطقي فقط، RTL+LTR وفاتح+داكن.

## 4. بنية الشرائح (FSD)

| الطبقة | الشريحة | المسؤولية |
| --- | --- | --- |
| shared | `config/driveIds.ts` | مرآة ثوابت `DriveCatalog` (space-type/kind/role/principal/version-status/source/deletion-status/session-status) |
| shared | `config/driveLimits.ts` | مرآة الحدود الافتراضية (حصة 5GB، حد ملف 200MB، امتدادات مسموحة) + `driveFileRejection(file)`؛ الخادم الحكم |
| shared | `api/httpClient.ts` (تعديل) | إضافة `putPresigned(url, file, { onProgress, contentType })` عبر axios مجرّد + إعادة استخدام `downloadBlob` |
| shared | `api/errorMessages.ts` + `labels/errors.ts` (تعديل) | 64 رمز `Drive.*` → `error.drive.*` عربية |
| shared | `labels/drive.ts` (توسيع) + `nav.ts` + `pages.ts` (تعديل) | نسخ الشاشات (أعمدة/حوارات/toasts/zod/شريط جانبي/معاينة/محرر) + `nav.drive` + `page.drive.*` |
| shared | `config/guides/drive.ts` + `index.ts` (تعديل) | دليل صفحة Drive |
| entities | `drive-space` | قراءات المساحات (list/get/overview) + الإعدادات (get)؛ أنواع `DriveSpaceResponse`/`DriveSpacesOverviewResponse`/`DriveSettingsResponse`؛ `driveSpaceKeys`؛ مقياس الحصة (ui) |
| entities | `drive-item` | قراءات التصفح/العنصر/breadcrumb/children/versions/permissions/effective/deletion-requests/trash؛ الأنواع + الشارات (kind/role/version-status/source/deletion-status/star)؛ `driveItemKeys` |
| features | `drive-space-manage` | إنشاء/تسمية/حصة مساحة + ensure-personal + تحديث الإعدادات (حوارات + zod) |
| features | `drive-item-manage` | إنشاء مجلد/تسمية/نقل/نجمة + استرجاع إصدار (حوارات + zod) |
| features | `drive-upload` | الرفع presigned (initiate→PUT→complete/abandon) + إصدار جديد + بصمة محلية + تقدّم + تحقق مسبق |
| features | `drive-sharing` | منح/سحب صلاحية + كسر/استعادة وراثة + بوابة الدور الفعلي (حوارات + zod) |
| features | `drive-deletion` | طلب حذف/موافقة/رفض/إلغاء/حذف مباشر + استعادة/حذف نهائي من السلة (حوارات + zod) |
| features | `drive-editor` | محرر OnlyOffice المضمّن (محمّل DocsAPI + DocEditor + edit-config + معالجة فشل الحفظ) |
| widgets | `drive-sidebar` | مبدّل المساحة + أوضاع ملفاتي/المشتركة/المفضلة/السلة/طلبات الحذف + مقياس الحصة |
| widgets | `drive-browser` | الشبكة/القائمة + breadcrumb + شريط أدوات + مرشحات + قوائم سياق + سحب وإفلات |
| widgets | `drive-preview-dialog` | معاينة صور/PDF/فيديو/Office (عارض OnlyOffice) |
| widgets | `drive-versions-panel` | سجل الإصدارات + تنزيل/استرجاع لكل إصدار |
| widgets | `drive-permissions-panel` | قائمة المنح + منح/سحب + كسر الوراثة + الدور الفعلي |
| widgets | `drive-deletion-inbox` | صندوق طلبات الحذف (خاصتي/للمراجعة/الكل لـ ADMIN) + موافقة/رفض/إلغاء |
| widgets | `drive-trash-panel` | شبكة السلة + استعادة/حذف نهائي |
| pages | `drive` | مسار `/drive` (RequirePermission DriveUse + GuideProvider + PageHeader + sidebar + browser) |
| app | `router/AppRouter.tsx` + `shared/config/routes.ts` + `widgets/app-shell/model/shell-nav.ts` (تعديل) | تسجيل المسار والمدخل |

> ملاحظة: تجميع القراءات في كيانيْن (`drive-space`/`drive-item`) على غرار `entities/archive` و
> `entities/document-file`؛ الكتابات موزّعة على features حسبConcern لتبقى كل شريحة صغيرة ومركّزة.

## 5. عقد الواجهة (المرجع الحرفي من الخلفية)

### 5.1 النقاط الطرفية (41)

**المساحات** `api/drive/spaces` (`DRIVE_USE`): `GET /` (overview: personal + shared)،
`POST /` (إنشاء مشتركة؛ تجاوز الحصة يحتاج `DRIVE_MANAGE` في المعالج)، `POST /personal` (ensure شخصي
idempotent)، `GET /{spaceId}`، `PUT /{spaceId}` (تسمية)، `PUT /{spaceId}/quota` (**`DRIVE_MANAGE`**؛
null = الافتراضي؛ 409 إن دون الاستخدام).

**العناصر** `api/drive/items` (`DRIVE_USE` + فحص وصول SQL): `GET /` (تصفح مصَفّح)،
`GET /{itemId}`، `GET /{itemId}/breadcrumb`، `GET /children?spaceId&parentId` (مصفوفة، مجلدات أولًا)،
`POST /folders` (Editor؛ الجذر owner/ADMIN)، `PUT /{itemId}/rename` (Editor)،
`PUT /{itemId}/move` (cycle-safe؛ بين المساحات Manager)، `POST /{itemId}/star` و`DELETE /{itemId}/star`
(Viewer، idempotent، بلا جسم).

**الملفات** `api/drive/files` (`DRIVE_USE`): `POST /initiate` (Editor؛ presigned PUT ‏15د)،
`POST /{versionId}/complete` (HEAD + magic bytes + SHA-256 + بصمة العميل الاختيارية)،
`DELETE /{versionId}/abandon`، `GET /{itemId}/content?versionId&disposition` (Range يدوي → 206 +
`Content-Range` + `Accept-Ranges: bytes`؛ `disposition=attachment` يفرض التنزيل)،
`GET /{itemId}/download-url?versionId` (presigned GET ‏5د)، `GET /{itemId}/versions` (مصفوفة، الأحدث
أولًا)، `GET /{itemId}/versions/{versionId}/content?disposition` (Range)،
`POST /{itemId}/versions/{versionId}/restore` (Editor؛ نسخ → إصدار جديد Source=Restore).

**الصلاحيات** `api/drive/items` (نفس البادئة، `DRIVE_USE`): `GET /{itemId}/permissions` (Manager؛
المنح الصريحة)، `POST /{itemId}/permissions` (Manager؛ upsert + إشعار)،
`DELETE /{itemId}/permissions/{permissionId}` (Manager)، `GET /{itemId}/effective` (يعيد `roleId`
‏0 عند انعدام الوصول — مثالي لبوابة الأزرار)، `PUT /{itemId}/inheritance` (Manager؛ break/restore).

**الحذف** `api/drive` (`DRIVE_USE`): `POST /items/{itemId}/deletion-requests` (سبب ≤500؛ 409 إن معلق)،
`GET /deletion-requests?statusId&mineOnly&page&pageSize` (ADMIN الكل؛ رئيس القسم ما يراجعه؛ mineOnly
خاصتي)، `POST /deletion-requests/{requestId}/approve|reject` (سلطة المراجعة؛ ملاحظة ≤500)،
`POST /deletion-requests/{requestId}/cancel` (الطالب فقط)، `DELETE /items/{itemId}` (مباشر: رئيس قسم
المالك أو ADMIN، وإلا 403).

**السلة** `api/drive/trash` (`DRIVE_USE`): `GET /?spaceId&search&page&pageSize` (الأحدث حذفًا أولًا)،
`POST /{itemId}/restore` (409 إن الأب مفقود/محذوف → جذر المساحة)، `DELETE /{itemId}` (نهائي: ADMIN/رئيس
قسم + Manage؛ يحذف الكائنات ويحرر الحصة).

**الإعدادات** `api/drive/settings` (**`DRIVE_MANAGE`** للمتوحّد كله): `GET /`، `PUT /`
(defaultQuotaBytes/maxFileSizeBytes/allowedExtensionsCsv).

**OnlyOffice** `api/drive` (`DRIVE_USE`): `GET /files/{itemId}/edit-config` (Viewer؛ config موقّع)،
`POST /onlyoffice/callback` (**`[AllowAnonymous]`** + JWT في الجسم؛ **دائمًا 200** `{error:0|1}` —
لا تستدعيه الواجهة).

### 5.2 معاملات التصفح (`GET items`)

`page` (افتراضي 1)، `pageSize` (افتراضي 20، clamp ‏1..100)، `spaceId?`, `parentId?` (null=الجذر)،
`search?` (اسم)، `kind?` (‏1 مجلد/2 ملف)، `ownerUserId?`, `trashed` (bool)، `starred` (bool)،
`modifiedFrom?/modifiedTo?` (UTC)، `sortBy?` (`name|modifiedAt|size`)، `sortDescending` (bool).
> أسماء الاستعلام تختلف عن الداخلية: `kind` لا itemKindId، `starred` لا starredOnly، `ownerUserId` لا
> ownerId، `sortDescending` bool لا sortDir. `versionId` استعلام في `content`/`download-url` ومسار في
> `versions/{id}/content` و`restore`؛ `complete`/`abandon` يأخذان **versionId** في المسار.

### 5.3 أجسام الطلب (`Contracts/Drive/DriveRequests.cs`)

`CreateDriveSpace{Name, QuotaBytes?}`، `RenameDriveSpace{Name}`، `SetDriveSpaceQuota{QuotaBytes?}`،
`CreateDriveFolder{SpaceId, ParentId?, Name}`، `RenameDriveItem{Name}`، `MoveDriveItem{NewParentId?}`،
`InitiateDriveUpload{SpaceId, ParentId?, FileName, DeclaredSizeBytes, MimeType?, FileItemId?}`،
`CompleteDriveUpload{ChecksumSha256?}`، `GrantDrivePermission{PrincipalTypeId, PrincipalId, RoleId}`،
`SetDriveItemInheritance{BreakInheritance}`، `RequestDriveItemDeletion{Reason}`،
`ReviewDriveDeletion{Note?}`، `UpdateDriveSettings{DefaultQuotaBytes, MaxFileSizeBytes,
AllowedExtensionsCsv?}`. بلا جسم: ensure-personal/star/unstar/abandon/restore-version/cancel/
delete-directly/trash-restore/purge.

### 5.4 استجابات DTO (camelCase حرفي)

- `DriveSpacesOverview{ personal: DriveSpace?, shared: DriveSpace[] }`
- `DriveSpace{ spaceId, name, spaceTypeId, ownerUserId, quotaBytes?, effectiveQuotaBytes, usedBytes, createdAt, createdByUserId, updatedAt?, trashedAt? }`
- `DriveItem{ itemId, spaceId, parentId?, itemKindId, name, ownerUserId, currentVersionId?, sizeBytes, breakInheritance, starred, trashedAt?, createdAt, createdByUserId, updatedAt? }` (`starred` لكل مستخدم)
- `DriveVersion{ versionId, fileItemId, versionNumber, storageProviderId, sizeBytes, checksumSha256?, mimeType, sourceId, statusId, isCurrent, createdByUserId, createdAt, committedAt? }`
- `DrivePermission{ permissionId, itemId, principalTypeId, principalId, roleId, grantedByUserId, grantedAt }`
- `DriveDeletionRequest{ requestId, itemId, requestedByUserId, reason, statusId, reviewedByUserId?, reviewedAt?, reviewNote?, createdAt }`
- `DriveSettings{ defaultQuotaBytes, maxFileSizeBytes, allowedExtensionsCsv }`
- `DriveBreadcrumbNode{ itemId, name, itemKindId }[]` (الجذر أولًا، العنصر نفسه آخرًا)
- `DriveUploadInitiated{ versionId, itemId, versionNumber, objectKey, uploadUrl, expiresAtUtc }`
- `DriveDownloadUrl{ url, expiresAtUtc }`
- `DriveEffectiveRole{ roleId }` (‏0 لا وصول/1 Viewer/2 Editor/3 Manager)
- `DriveEditConfig{ serverUrl, documentType: 'word'|'cell'|'slide', mode: 'edit'|'view', sessionId, versionNumber, config: Record<string,unknown> }` — يُمرَّر `config` حرفيًا لـ DocEditor (يحوي `document.{fileType,key,title,url,permissions}`, `editorConfig.{callbackUrl,lang:'ar',mode,user,customization:{forcesave,autosave}}`, `token`).
- `PagedResult<T>{ items, page, pageSize, totalCount, totalPages, hasPrevious, hasNext }`
- `DriveFileContentResult` **داخلي لا JSON** (stream + mimeType + fileName + totalSizeBytes + rangeStart?/rangeEnd?) — يُستهلك كـ blob/Range لا ككائن.

### 5.5 رموز الأخطاء (64 → `error.drive.*`)

NotFound→404، Validation→400، Conflict→409، Forbidden→403، Failure→500، Unauthorized→401.
المجموعات: Spaces (13: SpaceNotFound/SpaceNameEmpty/SpaceNameTooLong/SpaceTypeInvalid/
PersonalSpaceAlreadyExists/QuotaInvalid/QuotaBelowUsage/QuotaExceeded/UsageInvalid/SpaceTrashed/
SpaceAlreadyTrashed/SpaceNotTrashed/CannotTrashRoot)، Items (15: ItemNotFound/ItemNameEmpty/
ItemNameTooLong/ItemNameInvalid/NameTaken/DepthLimitReached/CycleDetected/ItemTrashed/
ItemAlreadyTrashed/ItemNotTrashed/NotAFile/NotAFolder/AccessDenied/DeletionPending/InheritanceBroken)،
Versions/Upload (16: VersionNotFound/VersionNumberInvalid/ObjectKeyEmpty/ProviderInvalid/
VersionSizeInvalid/ChecksumInvalid/VersionNotPending/VersionConflict/FileTooLarge/ExtensionNotAllowed/
SignatureMismatch/DeclaredSizeMismatch/StorageObjectMissing/MaxFileSizeInvalid/ExtensionsEmpty/
StorageNotConfigured)، Permissions (5)، Deletion (6)، Sessions (5: SessionNotFound/DocumentKeyEmpty/
DocumentKeyTooLong/SessionNotOpen/EditSessionExpired)، OnlyOffice (4: OnlyOfficeNotConfigured/
EditingNotSupported/CallbackTokenInvalid/CallbackDownloadFailed).
> انحرافات توثيقية يُعتمد فيها الكود: `DepthLimitReached` = Validation (400)، `DeclaredSizeMismatch`
> = Conflict (409). رمزا callback تُبتلع في `{error:1}` ‏200 فلا تراهما الواجهة مباشرة.

## 6. المراحل

> كل مرحلة تنتهي ببوابات: `npm run lint` (eslint + حارسا الأدلة/المفاتيح) + `npm run build`
> (`tsc -b && vite build`) نظيفان، و`dotnet build`/`dotnet test` للتأكد من عدم الانحدار الخلفي (لا
> تغيير خلفي)، مع README لكل مجلد ملامس + `knowledge.md` عند هبوط النقاط، ثم **توقف للمراجعة**.
> Phase 0 بلا واجهة مرئية فتُقبل بالبوابات؛ من Phase 1 يلزم التحقق في المتصفح (RTL+LTR، فاتح+داكن).
> التحقق الحيّ للبايتات/الرفع/المحرر يحتاج مفاتيح SeaweedFS (`Drive:Storage:*`) وعنوان OnlyOffice +
> `Drive:OnlyOffice:JwtSecret` (user-secrets) — غير متاحة بعد؛ البديل headless موثّق لكل مرحلة (§8).

### Phase 0 — الأساس المقروء (بلا واجهة مرئية) ✅ منفّذة (2026-11-20)

- **المخرجات:**
  - `shared/config/driveIds.ts`: مرآة `DriveCatalog` (SpaceType 1/2، ItemKind 1/2، Role 1/2/3،
    PrincipalType 1/2، VersionStatus 1/2/3، VersionSource 1/2/3، DeletionStatus 1..4، SessionStatus 1..4).
  - `shared/config/driveLimits.ts`: `DEFAULT_QUOTA_BYTES` (5GB)، `MAX_FILE_BYTES` (200MB)،
    `ALLOWED_EXTENSIONS` مطابقة للبذر + `driveFileRejection(file)` (فارغ/حجم/امتداد) برسائل عربية
    و`withCode`؛ تعليق يشير إلى أن الخادم الحكم والإعدادات بـ `DRIVE_MANAGE`.
  - `shared/api/httpClient.ts`: `putPresigned(url, file, { onProgress, contentType })` عبر axios مجرّد
    (بلا معترضات/بلا baseURL) + إبقاء `downloadBlob`/`uploadForm`.
  - `shared/api/errorMessages.ts` + `labels/errors.ts`: كتلة `// Module 11 — Drive` بكل الرموز الـ64
    → `error.drive.*` (ترتيب أبجدي داخل الكتلة).
  - `labels/drive.ts`: توسيع بنسخ الشاشات (أعمدة الشبكة، حوارات المجلد/النقل/التسمية/المساحة/الحصة/
    الصلاحيات/الحذف/السلة/الإصدارات/المعاينة/المحرر، toasts، رسائل zod، الشريط الجانبي، عداد الحصة)؛
    `labels/nav.ts`: `nav.drive`؛ `labels/pages.ts`: `page.drive.title/.description`.
  - `entities/drive-space`: `model/types.ts` (Space/Overview/Settings)، `api/driveSpaceApi.ts`
    (list/get/overview/settings-get)، `model/queries.ts` (`driveSpaceKeys` + hooks بـ`enabled`/
    `placeholderData`/`staleTime`)، `ui/QuotaMeter.tsx`، `index.ts`.
  - `entities/drive-item`: `model/types.ts` (Item/Version/Permission/DeletionRequest/BreadcrumbNode/
    EffectiveRole + `PagedResult`)، `api/driveItemApi.ts` (browse/get/breadcrumb/children/versions/
    permissions/effective/deletion-requests/trash/content-blob/download-url)، `model/queries.ts`
    (`driveItemKeys` بمفاتيح user-scoped حيث يلزم + hooks)، `ui/` شارات (ItemKindIcon/RoleBadge/
    VersionStatusBadge/VersionSourceBadge/DeletionStatusBadge/StarIndicator)، `index.ts`.
  - `knowledge.md`: تحديث سطر Module 11 إلى «live» بقائمة النقاط (يُستكمل مع هبوط الشرائح).
- **قبول:** lint + build أخضران؛ تحذيرات orphan لـ`drive.*` تنخفض كلما تُستهلك المفاتيح؛ لا واجهة
  بعد ولا مدخل تنقّل؛ لا تستدعي أي صفحة الخطافات.
- **لمس الخلفية:** لا.
- **نتيجة التنفيذ (2026-11-20):** كل المخرجات أعلاه نُفّذت. `driveSpaceApi` = `overview`/`get`/`settings`
  (لا `list` منفصل؛ الـoverview يعيد الشخصية+المشتركة). `driveItemKeys` **ليست user-scoped** — تتبع
  سابقة `archive` (الخادم يرشّح حسب وصول المتصل في كل قراءة). `useDrivePermissions`/`useDriveSettings`
  opt-in (`enabled: false` افتراضًا). `drive.ts` = 185 مفتاحًا، `error.drive.*` = 64. البوابات:
  `npm run lint` exit 0 (159 تحذير orphan متوقعة)، `npm run build` أخضر. لا واجهة/مسار/مدخل تنقّل بعد.

### Phase 1 — الهيكل والتنقّل والمساحات وعداد الحصة ✅ منفّذة (2026-11-20)

- **المخرجات:**
  - `shared/config/routes.ts`: `DRIVE: '/drive'`؛ `AppRouter.tsx`: `<Route path={ROUTES.DRIVE}
    element={<DrivePage/>}/>` داخل ProtectedRoute→AppShell.
  - `shell-nav.ts`: مدخل `{ id:'drive', labelKey:'nav.drive', to:ROUTES.DRIVE, icon:FolderOpen,
    permission:Permissions.DriveUse, available:true }` في المجموعة المناسبة.
  - `pages/drive`: `RequirePermission DriveUse` + `GuideProvider pageId="drive"` + `PageHeader
    titleKey/descriptionKey` + `<DriveSidebar/>` + `<DriveBrowser/>` (عنصر نائب في هذه المرحلة).
  - `widgets/drive-sidebar`: مبدّل المساحة (overview: شخصية + مشتركة可达)، أوضاع
    ملفاتي/المشتركة/المفضلة/السلة/طلبات الحذف في الرابط، `QuotaMeter` للمساحة النشطة، زر «مساحة
    جديدة» (يُفعّل في Phase 2/5).
  - `features/drive-space-manage`: `ensurePersonal` (يُستدعى عند أول دخول لملفاتي)، حوار إنشاء مساحة
    مشتركة + تسمية + (حصة لـ ADMIN) — zod + إبطال `driveSpaceKeys`.
  - دليل `guides/drive.ts` (مناطق sidebar/quota) + تسجيله.
- **قبول:** المتصفح: التنقّل إلى `/drive`، ظهور المساحة الشخصية (ensure)، سرد المشتركة可达، تبديل
  الأوضاع يحدّث الرابط، عداد الحصة صحيح، إخفاء المدخل عمن لا يملك `DRIVE_USE`، حالة ممنوع مخصّصة.
  RTL+LTR وفاتح+داكن.
- **لمس الخلفية:** لا.
- **النتيجة (2026-11-20):** `routes.DRIVE` + مسار `DrivePage` داخل `AppShell` + مدخل «الملفات» في
  مجموعة المراسلات (`shell-nav`). `pages/drive` (`RequirePermission DriveUse` + `GuideProvider pageId="drive"`
  + `PageHeader` + `DriveWorkspace`) مع حالة تنقّل في الرابط `useDriveNavigation` (view/space/folder).
  `widgets/drive-sidebar`: أوضاع خمسة، مبدّل مساحة شخصية/مشتركة بقائمة صف (تسمية للمالك/`DRIVE_MANAGE`،
  حصة لـ`DRIVE_MANAGE`)، `QuotaMeter` للمساحة النشطة، ضمان الشخصية كسول عند أول دخول.
  `features/drive-space-manage`: ensure/create/rename/quota (zod + `useDraftForm` + إبطال `driveSpaceKeys`)
  وحواراتها الثلاثة. `DriveBrowser` عنصر نائب حتى Phase 2. دليل `guides/drive.ts` بأربع مناطق
  (sidebar/spaces/quota/browser) مسجّلة. الكتالوج 185→188 مفتاحًا. البوابات الأمامية: lint exit 0
  (135 orphan متوقعة) + build أخضر؛ لا تغيير خلفي. تحقق المتصفح مؤجّل لجولة Phase 6 (تسجيل دخول يدوي).

### Phase 2 — التصفح (شبكة/قائمة) + breadcrumb + عمليات المجلدات + السحب والإفلات ✅ منفّذة (2026-11-20)

- **المخرجات:**
  - `widgets/drive-browser`: `DataTable mode="server"` بمعرّف `drive-items`، أعمدة
    (الاسم+أيقونة النوع، الحجم، المالك، معدّل في، نجمة) قابلة للإخفاء مع عمود معرّف ثابت، مبدّل
    شبكة/قائمة، بحث/فرز/ترقيم/مرشحات في الرابط (`useTableState`)، breadcrumb من
    `GET breadcrumb`، شريط أدوات (مجلد جديد/رفع/تحديث)، قوائم سياق صف/رأس/فارغ مفلترة بالدور الفعلي.
  - `features/drive-item-manage`: حوارات إنشاء مجلد/تسمية/نقل (شجرة مجلدات من `children`) + نجمة/
    إزالة نجمة (تحديث تفاؤلي لـ`starred`) + zod؛ إبطال `driveItemKeys.browse/detail/children`.
  - السحب والإفلات: سحب صف إلى مجلد ⇒ `move`؛ إفلات ملفات على المتصفح ⇒ يُمرَّر إلى `drive-upload`
    (Phase 3) — في هذه المرحلة إفلات الملفات يُظهر رسالة «الرفع في Phase 3» أو يُفعّل مع Phase 3.
    بديل حوار «نقل» للوحة المفاتيح/اللمس.
  - بوابة الدور الفعلي: `useEffectiveRole(itemId/parent)` لتعطيل/إظهار إجراءات الصف والشريط.
- **قبول:** المتصفح: تصفح مجلد/ملف، ترقيم/فرز/بحث/مرشحات في الرابط، breadcrumb تنقّل، إنشاء/تسمية/
  نقل مجلد، نجمة، سحب-إفلات نقل + بديل لوحة المفاتيح، قوائم سياق مفلترة، حالات تحميل/فارغ/خطأ.
- **لمس الخلفية:** لا.
- **النتيجة (2026-11-20):** `widgets/drive-browser` — `DataTable mode="server"` (`drive-items`) + شبكة بطاقات
  تتشاركان حالة الرابط/القوائم، breadcrumb (عُقد قابلة للنقر وأهداف إفلات)، شريط أدوات (بحث مؤجّل/
  مبدّل شبكة-قائمة/مجلد جديد/رفع/مرشّح نوع/مالك/تحديث)، بوابة الدور الفعلي عبر `useItemRoles`
  (`useQueries` دفعة `/effective`)، سحب-إفلات نقل (مصدر داخل خلية الاسم + أهداف مجلد/breadcrumb) وبديل
  حوار «نقل» بشجرة مجلدات كسولة. `features/drive-item-manage`: create-folder/rename/move/star
  (نقل/نجمة متغيّرة، نجمة تفاؤلية) + `itemNameSchema` يعكس `DriveItem` + 3 حوارات + `FolderTreePicker`.
  `DriveItemActions` نقاط تمديد للمراحل 3–5؛ إفلات الملفات يُمرَّر إلى `onDropFiles` (Phase 3).
  الكتالوج 188→204. البوابات الأمامية: lint exit 0 (100 orphan) + build أخضر؛ لا تغيير خلفي.
  التحقق البصري مؤجّل لجولة Phase 6.

### Phase 3 — الرفع presigned + التنزيل + المعاينة ✅ منفّذة (2026-11-20)

- **المخرجات:**
  - `features/drive-upload`: حوار رفع (متعدد الملفات تسلسليًا) + إصدار جديد لملف قائم
    (`FileItemId`)؛ التدفق initiate→`putPresigned`→complete (بصمة محلية اختيارية عبر
    `useLocalChecksum`) / abandon عند الفشل؛ شريط تقدّم ثلاثي (تحضير/رفع {n}%/تحقق) على غرار
    المرفقات؛ تحقق مسبق `driveFileRejection` + الحصة المتبقية؛ `accept` من الامتدادات المسموحة.
  - تنزيل: `download-url` presigned (anchor) أو `downloadBlob` + `parseContentDisposition` +
    `saveBlobAsFile` (أسماء عربية).
  - `widgets/drive-preview-dialog`: صور/PDF عبر object-URL من `content`؛ فيديو عبر `download-url`
    (Range)؛ Office عبر عارض OnlyOffice (Phase 6) أو تنزيل؛ غير المدعوم ⇒ تنزيل برسالة عربية.
  - إفلات الملفات على المتصفح ⇒ رفع إلى المجلد الحالي (يكتمل DnD من Phase 2).
- **قبول:** المتصفح (يحتاج مفاتيح SeaweedFS للبايتات الحيّة): رفع ملف → يظهر في الشبكة بحجم/إصدار
  صحيح، تقدّم، بصمة، تنزيل باسم عربي، معاينة صورة/PDF/فيديو، رفض امتداد/حجم/حصة مسبقًا برسالة عربية،
  abandon عند الإلغاء. **بديل headless** عند غياب المفاتيح: فحص منطق التحقق المسبق/التدفق في Node
  مقابل وحدات حقيقية + توثيق أن المسار الحيّ غير موثّق بصريًا (كما في المرفقات).
- **لمس الخلفية:** لا.
- **النتيجة (2026-11-20):** `features/drive-upload` — `driveUploadApi` (initiate/complete/abandon) +
  `useUploadDriveFileMutation` (تدفق initiate→`putPresigned`→complete، بصمة SHA-256 محلية اختيارية،
  abandon عند الفشل، `DriveUploadPutError` لفشل PUT بوابة) + `UploadDriveDialog` (طابور تسلسلي متعدّد
  الملفات، إصدار جديد لملف قائم عبر `FileItemId`، شريط تقدّم ثلاثي تحضير/رفع {n}%/تحقق، رفض مسبق
  `driveFileRejection` + الحصة المتبقية، `accept` من الامتدادات، إلغاء يوقف بعد الملف الحالي) +
  `downloadDriveFile` (`downloadBlob` + `parseContentDisposition` + `saveBlobAsFile` بأسماء عربية).
  **تطابق Content-Type:** يُحسب `file.type || 'application/octet-stream'` مرة ويُمرَّر إلى initiate
  (`mimeType`) و`putPresigned` (`contentType`) معًا لأن التوقيع يغطّي النوع. `widgets/drive-preview-dialog`:
  صور/PDF/فيديو/صوت عبر `download-url` presigned (`useDriveDownloadUrl` جديد، `staleTime`/`gcTime` 0)،
  Office مصنّف لعارض Phase 6، غير المدعوم ⇒ تنزيل برسالة عربية. ربط `DriveWorkspace` (رفع/إفلات/إصدار
  جديد/تنزيل/فتح-معاينة) و`DriveItemActions.onUploadVersion`. الكتالوج 204→216. البوابات الأمامية:
  lint exit 0 (91 orphan) + build أخضر؛ لا تغيير خلفي. البايتات الحيّة تحتاج مفاتيح SeaweedFS —
  التحقق البصري مؤجّل لجولة Phase 6 (فحص منطق headless كما في المرفقات).

### Phase 4 — الإصدارات + الصلاحيات ✅ منفّذة (2026-11-21)

- **المخرجات:**
  - `widgets/drive-versions-panel`: حوار/لوحة تسرد `versions` (الأحدث أولًا، شارة الحالي/المصدر/
    الحالة)، تنزيل لكل إصدار (`versions/{id}/content`)، استرجاع (Editor+) ⇒ نسخ → إصدار جديد +
    إبطال + toast.
  - `widgets/drive-permissions-panel` + `features/drive-sharing`: قائمة المنح الصريحة (Manager)،
    حوار منح (مستخدم/قسم + دور) مع منتقي مستخدم/قسم (من الكيانات الموجودة)، سحب، كسر/استعادة وراثة،
    عرض الدور الفعلي للمستخدم الحالي؛ شارة «موروثة»؛ zod + إبطال `driveItemKeys.permissions/effective`.
- **قبول:** المتصفح: سجل إصدارات + استرجاع ينشئ إصدارًا جديدًا، منح/سحب صلاحية لمستخدم/قسم، كسر
  وراثة يغيّر الوصول، بوابة Manager تخفي لوحة الصلاحيات عن Editor/Viewer، إشعار المنح يصل.
- **لمس الخلفية:** لا.

- **النتيجة (2026-11-21):** أُضيفت لوحة سجل الإصدارات والتنزيل والاسترجاع كإصدار جديد، وشرائح
  مشاركة Drive لمنح/سحب صلاحيات المستخدمين والأقسام وكسر/استعادة الوراثة، وربطت في `DriveWorkspace`
  مع بوابة الدور الفعلي. `npm run build` أخضر؛ التحقق الحي مؤجل لجولة المتصفح في Phase 6؛ لا تغيير خلفي.

### Phase 5 — الحذف والسلة وصندوق المراجعة + الإعدادات (ADMIN)

**الحالة: منفذة.** تم التحقق عبر `npm run build` و`npm run lint`؛ التحقق الحي مؤجل إلى Phase 6.

- **المخرجات:**
  - `features/drive-deletion`: حوار طلب حذف (سبب إجباري ≤500) + إلغاء الطالب؛ موافقة/رفض (ملاحظة
    ≤500) لسلطة المراجعة؛ حذف مباشر حيث يسري (ADMIN/رئيس قسم + Manage)؛ استعادة من السلة + حذف نهائي
    (تأكيد مدمّر).
  - `widgets/drive-deletion-inbox`: شبكة مصَفّحة لطلبات الحذف (mineOnly/للمراجعة/الكل لـ ADMIN) +
    موافقة/رفض/إلغاء + شارات الحالة.
  - `widgets/drive-trash-panel`: شبكة السلة (`GET trash`) + استعادة/حذف نهائي + تنبيه احتساب الحصة.
  - `features/drive-space-manage` (إكمال): حوار إعدادات Drive (`DRIVE_MANAGE`: حصة افتراضية/حد ملف/
    امتدادات) + تجاوز حصة مساحة؛ قراءة الإعدادات للتحقق المسبق حيث تتاح.
- **قبول:** المتصفح: طلب حذف → إشعار للمعتمِد → موافقة تنقل للسلة → استعادة/حذف نهائي يحرر الحصة؛
  رفض/إلغاء؛ حذف مباشر للرئيس/ADMIN؛ صندوق المراجعة مصفّى حسب الدور؛ الإعدادات تُحدّث الحدود وتنعكس
  على التحقق المسبق. RTL+LTR وفاتح+داكن.
- **لمس الخلفية:** لا.

### Phase 6 — المحرر المضمّن OnlyOffice + الأدلة + التحقق الشامل والإغلاق

**الحالة: منفذة برمجيًا.** build وlint أخضران؛ التحقق الحي للمحرر والبايتات مؤجل حتى توفير
OnlyOffice وSeaweedFS، مع توثيق المسار البديل headless.

- **المخرجات:**
  - `features/drive-editor`: محمّل DocsAPI كسول من `serverUrl`، مكوّن محرر يركّب `DocEditor` بـ`config`
    حرفيًا من `edit-config`، وضع edit/view حسب الدور/الامتداد، `destroy()` عند الإغلاق، معالجة أحداث
    المحرر (خطأ/حفظ) + toast، فشل الحفظ يصل كإشعار (النوع 17) في صندوق الإشعارات؛ معاينة Office عبر
    العارض (view) حين مضبوط. رسالة عربية واضحة عند `OnlyOfficeNotConfigured`/`EditingNotSupported`.
  - إكمال دليل `guides/drive.ts` (grid/breadcrumb/upload/versions/permissions/editor + أدوات `table.*`).
  - تحقق متصفح شامل بثلاثة أدوار (ADMIN/مستخدم عادي/Viewer) + عيّنة مستوى وصول أدنى، RTL+LTR،
    فاتح+داكن، قوائم سياق/لوحة مفاتيح، حالات loading/empty/error/forbidden.
  - توثيق نهائي: READMEs كل الشرائح + `knowledge.md` (النقاط الحيّة كاملة) + مزامنة عدد الصلاحيات في
    `AGENTS.md` (25) + جذر README (صف الموديول 11 واجهة) + هذه الخطة (الحالة + Change Log).
- **قبول:** المتصفح (يحتاج OnlyOffice + SeaweedFS): فتح محرر Word/Excel مضمّن، تحرير → حفظ → إصدار
  جديد (Source=EditorSave)، وضع view للقارئ، فشل الحفظ → إشعار، معاينة Office. **بديل headless** عند
  غياب الضبط: فحص تركيب config/المحمّل في Node + توثيق أن الجلسة الحيّة غير موثّقة. البوابات الأربع
  خضراء (lint/build أمامي، build/test خلفي بلا انحدار).
- **لمس الخلفية:** لا.

## 7. خارج النطاق صراحةً

- أي نقطة نهاية جديدة أو أي تغيير خلفي (الخلفية مغلقة `ff04421`)؛ بما فيه إتاحة `settings` للقراءة
  العادية أو كشف مستوى الوصول في استجابة التصفح — تُعالج بمرآة/`effective` وتُسجَّل كتحسين مقترح.
- البحث في **محتوى** الملفات (المرحلة اللاحقة المؤجلة في الخلفية)؛ البحث بالاسم فقط.
- الروابط الخارجية/المشاركة العامة (لا وجود لها في العقد)؛ الصلاحيات داخلية (مستخدم/قسم).
- استئناف الرفع المقطوع (presigned PUT بلا استئناف)؛ رفع جماعي متوازٍ كبير (تسلسلي معقول).
- transcoding الفيديو (Range فقط كما في الخلفية).
- إدارة القواميس العامة، الأرشفة (07)، العلاقات (08)، الإحصاءات.
- إشعارات Drive الحيّة عبر SignalR (الصندوق الحالي يعرض إشعارات Outbox؛ لا hub مخصّص لـ Drive).
- تحرير تعاوني متعدد المؤشرات خارج ما يوفره OnlyOffice نفسه (الغرفة المشتركة لكل مفتاح موجودة خلفيًا).

## 8. المخاطر والقرارات

| الخطر/القرار | المعالجة المقترحة |
| --- | --- |
| CORS لـ PUT presigned من أصل الواجهة إلى SeaweedFS | اعتمادية نشر: ضبط CORS على البوابة للسماح بـ PUT من أصل SPA؛ يُوثّق في README الرفع؛ التحقق الحيّ يحتاج المفاتيح |
| رؤوس PUT (Content-Type) يجب أن تطابق التوقيع | فحص `S3DriveStorageService` في Phase 3؛ إرسال نفس النوع الذي وقّعه الخادم أو إغفاله إن لم يُوقَّع؛ نقطة مفتوحة §9 |
| انتهاء صلاحية presigned (رفع 15د/تنزيل 5د) | إعادة جلب الرابط عند الانتهاء/الفشل؛ رسالة عربية «انتهت صلاحية الرابط، أعد المحاولة» |
| `<video>`/`<img>` لا يرفق JWT | الوسائط عبر `download-url` presigned؛ الصور/PDF عبر blob معترَض؛ لا كشف رموز في الرابط |
| غياب مفاتيح SeaweedFS/OnlyOffice للتحقق الحيّ | بديل headless موثّق لكل مرحلة (فحص المنطق في Node) + توثيق أن المسار الحيّ غير موثّق بصريًا، كما في المرفقات |
| انحراف مرآة الحدود/الثوابت عن البذر | تعليق يشير إلى المصدر (`DriveCatalog.cs`/البذر)، الخادم الحكم وأخطاؤه عربية، والإعدادات الحيّة تُقرأ حيث تتاح (ADMIN) |
| أسماء عربية في Content-Disposition | `filename*=UTF-8''` أولًا ثم `filename=` ثم اسم احتياطي (المساعد موجود من المرفقات) |
| خطأ التنزيل يعود blob فلا يُطبَّع | `downloadBlob` يقرأ JSON من الـ blob ويطبّعه (موجود)؛ يُعاد استخدامه |
| حساب بصمة ملف كبير (200MB) في الذاكرة | `useLocalChecksum` يعطّل الزر أثناء الحساب + مؤشر؛ البصمة اختيارية في `complete` (الخادم يحسبها دفقًا)؛ توثيق القيد |
| السحب والإفلات الأصلي وإمكانية الوصول | بديل حوار «نقل» دائمًا + قوائم سياق لوحة المفاتيح؛ DnD تحسين لا مسار وحيد |
| بوابة الدور الفعلي تتطلب نداء `effective` لكل عنصر | جلبه للمجلد الحالي/العنصر المحدد (لا لكل صف)؛ caching عبر React Query؛ الخادم يحكم نهائيًا عند الفعل |
| OnlyOffice غرفة مشتركة لكل مفتاح (تعاون) | reuse الجلسة خلفيًا؛ الواجهة تعرض وضع edit/view فقط؛ فشل الحفظ (تضارب) يصل كإشعار لا كخطأ نقل |
| `callback` دائمًا 200 `{error:0|1}` | الواجهة لا تستدعيه؛ لا تعتمد على HTTP status لإشارة فشل الحفظ بل على الإشعار/أحداث المحرر |
| تبديل الوضع/المساحة يصفّر استعلام الشبكة | سلوك مقصود (URL-driven)؛ `resetOnChange` في `useTableState`؛ يُوثّق في README الصفحة |
| لا مشغّل اختبارات أمامي | الاعتماد على حارسي lint + build + تحقق المتصفح الإلزامي من Phase 1 + بدائل Node headless |

## 9. نقاط مفتوحة تحتاج قرارًا (المقترح أولًا)

| # | النقطة | المقترح | البديل |
| --- | --- | --- | --- |
| 1 | مكتبة السحب والإفلات | HTML5 أصلي + حوار نقل بديل (لا تبعية جديدة) | مكتبة مثل dnd-kit (تبعية + حجم) |
| 2 | معاينة Office | عارض OnlyOffice (view) حين مضبوط، وإلا تنزيل | تنزيل فقط دائمًا (أبسط، لكن بلا معاينة) |
| 3 | معاينة الفيديو | `download-url` presigned كـ`<video src>` (Range من SeaweedFS) | blob معترَض (لا seeking جيد) |
| 4 | صندوق طلبات الحذف | وضع `view=deletion` داخل `/drive` | صفحة/مسار منفصل |
| 5 | حجم الصفحة الافتراضي | 10 (عرف الشبكة القائم) | 20 (افتراضي الخادم) |
| 6 | مرآة الحدود مقابل قراءتها حيّة | مرآة ثابتة للمستخدم العادي + قراءة حيّة لـ ADMIN في الإعدادات | انتظار إتاحة `settings` للقراءة العادية (تغيير خلفي — مرفوض) |
| 7 | Content-Type في PUT presigned | يُحسم بفحص `S3DriveStorageService` في Phase 3 | — |

## 10. تسلسل التنفيذ والبوابات

```
Phase 0 → Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
```

- لا تغيير خلفي في أي مرحلة؛ البوابات الخلفية (`dotnet build`/`dotnet test`) تُشغّل فقط للتأكد من عدم
  الانحدار (المرجع 1264 اختبارًا). قبل أي بناء خلفي: التأكد أن خادم التطوير (منفذ 5028) غير مشغّل،
  وإن كان جلسة VS debug يُسأل المستخدم قبل إيقافه.
- بوابات كل مرحلة: `npm run lint` + `npm run build` نظيفان + READMEs الملامسة + `knowledge.md` عند
  هبوط النقاط + لا أسرار في الكود (مفاتيح SeaweedFS/OnlyOffice في user-secrets لا في appsettings/الكود).
- **التزام مراجعة بعد كل مرحلة قبل بدء التالية**، والتحقق في المتصفح إلزامي من Phase 1 (RTL+LTR،
  فاتح+داكن، ثلاثة أدوار في Phase 6). التحقق الحيّ للبايتات/المحرر مشروط بتوفير مفاتيح SeaweedFS
  وعنوان OnlyOffice + JWT secret؛ وإلا بديل headless موثّق.
- لا commits إلا بطلب صريح من المستخدم.

## Change Log

| التاريخ | الكاتب | التغيير |
| --- | --- | --- |
| 2026-11-21 | Copilot | تنفيذ Phase 6 برمجيًا: قراءة `edit-config`، محمّل DocsAPI كسول، محرر OnlyOffice مع تنظيف `destroy()` وأحداث الخطأ/الإغلاق، معاينة Office، دليل Drive، مزامنة الوثائق والصلاحيات. build وlint أخضران؛ التحقق الحي مؤجل لغياب إعدادات OnlyOffice/SeaweedFS؛ لا تغيير خلفي. |
| 2026-11-21 | Copilot | تنفيذ Phase 5: طلبات الحذف والمراجعة، السلة والاستعادة/الحذف النهائي، الحذف المباشر، وإعدادات Drive؛ ربطها بـ`DriveWorkspace` وتحديث READMEs. build وlint أخضران؛ لا تغيير خلفي. |
| 2026-11-21 | Copilot | تنفيذ Phase 4: سجل الإصدارات والتنزيل والاسترجاع، مشاركة المستخدم/القسم، سحب الصلاحية، وكسر/استعادة الوراثة؛ ربط workspace وتحديث READMEs. build أخضر؛ lint والتحقق الحي التاليان. |
| 2026-11-20 | Qoder | تنفيذ Phase 3 (الرفع/التنزيل/المعاينة): `features/drive-upload` (`driveUploadApi` + `useUploadDriveFileMutation` initiate→`putPresigned`→complete/abandon + بصمة محلية + `DriveUploadPutError`، `UploadDriveDialog` طابور تسلسلي/إصدار جديد/تقدّم ثلاثي/رفض مسبق/حصة، `downloadDriveFile` بأسماء عربية)؛ تطابق Content-Type بين initiate وPUT؛ `widgets/drive-preview-dialog` (صور/PDF/فيديو/صوت عبر `download-url` presigned + `useDriveDownloadUrl` جديد، Office لـPhase 6، غير المدعوم ⇒ تنزيل)؛ ربط `DriveWorkspace` + `DriveItemActions.onUploadVersion`؛ الكتالوج 204→216. READMEs للشرائح + الأمهات. البوابات الأمامية: lint exit 0 (91 orphan) + build أخضر؛ لا تغيير خلفي. تنفيذ متواصل نحو Phase 4. |
| 2026-11-20 | Qoder | تنفيذ Phase 2 (التصفح): `widgets/drive-browser` (`DataTable` server `drive-items` + شبكة بطاقات، breadcrumb، أدوات/مرشّحات نوع/مالك، بوابة الدور الفعلي `useItemRoles`، سحب-إفلات نقل + حوار نقل بشجرة كسولة، `DriveItemActions` للمراحل 3–5)؛ `features/drive-item-manage` (create-folder/rename/move/star متغيّرة + نجمة تفاؤلية + `itemNameSchema` + 3 حوارات + `FolderTreePicker`)؛ ربط `DriveWorkspace`؛ الكتالوج 188→204. READMEs للشرائح + الأمهات. البوابات الأمامية: lint exit 0 (100 orphan) + build أخضر؛ لا تغيير خلفي. تنفيذ متواصل نحو Phase 3. |
| 2026-11-20 | Qoder | تنفيذ Phase 1 (الهيكل والتنقّل والمساحات والحصة): `routes.DRIVE` + مسار `DrivePage` + مدخل «الملفات» في مجموعة المراسلات؛ `pages/drive` (`RequirePermission`/`GuideProvider`/`PageHeader`/`DriveWorkspace`) مع `useDriveNavigation` (view/space/folder في الرابط) وضمان الشخصية الكسول؛ `widgets/drive-sidebar` (أوضاع خمسة + مبدّل مساحة بقائمة صف + `QuotaMeter`)؛ `features/drive-space-manage` (ensure/create/rename/quota + zod + `useDraftForm` + 3 حوارات)؛ `DriveBrowser` عنصر نائب؛ `guides/drive.ts` (4 مناطق)؛ الكتالوج 185→188. `DriveView`/`isDriveView` في `entities/drive-item`. READMEs للشرائح الأربع + الأمهات. البوابات الأمامية: lint exit 0 (135 orphan) + build أخضر؛ لا تغيير خلفي. تنفيذ متواصل نحو Phase 2 بطلب المستخدم. |
| 2026-11-20 | Qoder | تنفيذ Phase 0 (الأساس المقروء، بلا واجهة): `driveIds.ts` + `driveLimits.ts` (+تصدير عبر الحاجز)، `putPresigned` في `httpClient.ts`، 64 رمز `Drive.*` → `error.drive.*`، توسيع `drive.ts` إلى 185 مفتاحًا + `nav.drive`/`page.drive.*`، وشريحتا `entities/drive-space` (overview/get/settings + `QuotaMeter`) و`entities/drive-item` (browse/get/breadcrumb/children/versions/permissions/effective/deletion-requests/trash/download-url + قراءتا بايت + 6 شارات). READMEs لكل مجلد ملامس + `knowledge.md` سطر Module 11. تصحيح عدد رموز الأخطاء 51→64 (المصدر `DomainErrors.cs`). البوابات: lint exit 0 (159 orphan متوقعة) + build أخضر؛ لا تغيير خلفي. توقف للمراجعة قبل Phase 1. |
| 2026-11-20 | Qoder | إنشاء خطة واجهة الموديول 11 (Drive): مسح خلفي كامل (41 نقطة/DTOs/64 رمز خطأ/تدفقات presigned وOnlyOffice) ومسح أمامي (FSD/شبكة/تسميات/رفع/أدلة)، وبنية شرائح مقترحة و7 مراحل (0–6) ونقاط مفتوحة ومخاطر، بانتظار الاعتماد. لا تنفيذ. |