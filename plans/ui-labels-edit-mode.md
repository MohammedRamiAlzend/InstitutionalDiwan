# الخطة — قاموس التسميات القابل للتعديل + وضع التعديل المباشر (UI Labels)

**الحالة:** معتمدة — **المراحل 0–5 منفَّذة (2026-11-17/18)**؛ كل نص الواجهة الثابت يُحل من الكتالوج (~3006 مفتاح)، وحارس الاتساق `scripts/check-label-keys.mjs` موصول ببوابة lint. **متبقٍ فقط:** جولة المتصفح الشاملة بثلاثة أدوار فاتح/داكن (دخول يدوي من المستخدم). لا commits إلا بطلب.
**النطاق:** واجهة SPA (`institutionaldiwan.front`) + طرف API جديد صغير في `InstitutionalDiwan.Server` (جدول واحد + 3 endpoints). لا تعديل على نصوص السيرفر المولّدة (إشعارات/تدقيق/أخطاء تحقق) — خارج النطاق عمداً، والتصميم لا يمنع إضافتها لاحقاً.
**آخر مراجعة:** 2026-11-18.
**مرجع الأسلوب:** [plans/tutorial-mode-frontend.md](tutorial-mode-frontend.md) — نفس بنية الأقسام والمراحل ومعايير التحقق.
**قرارات المستخدم المعتمدة (2026-11-17):** الواجهة فقط كبداية · وضع تعديل مباشر Inline · **الكتابة لحساب `superadmin` حصراً** (لا صلاحية قابلة للمنح عبر إدارة الأدوار).

---

## 1. الوضع الحالي المؤكَّد (تم فحصه)

- **النصوص مكتوبة مباشرة في المكونات**: ~250 ملف `.ts/.tsx` تحوي نصاً عربياً، و~3188 سلسلة نصية عربية (مع التكرار). لا يوجد أي قاموس أو طبقة i18n اليوم.
- **نقاط إلصاق موحّدة موجودة** (نفس ما وثّقه الوضع التعليمي):
  - كل الجداول تمرّ عبر `shared/ui/data-table.tsx` (رؤوس الأعمدة، شريط الأدوات، القوائم).
  - كل الحقول عبر `shared/ui/form-field.tsx` (`label`).
  - عناوين الصفحات عبر `shared/ui/page-header.tsx` (`title`, `description`).
  - الأزرار عبر `shared/ui/button.tsx`، والتأكيد عبر `confirm-dialog.tsx`.
- **الـ sidebar** معرّف تصريحياً في `widgets/app-shell/model/shell-nav.ts`: 5 مجموعات و17 عنصراً، كل منها `id` + `label` ثابتان — ترحيلها سهل ومحصور بملف واحد + عارض القائمة.
- **نمط التفضيلات المحلية جاهز للتقليد**: `shared/lib/theme.ts` و`tutorial.ts` (localStorage + `useSyncExternalStore` + حسم مبكر في `index.html`).
- **الإقلاع**: `app/providers/AuthSessionLoader.tsx` يجلب `GET /api/auth/me` عند وجود token ويملأ `useSessionStore`. لا توجد شاشة loading عامة حالياً ولا Suspense fallback في الموجّه.
- **الكيان المرجعي للطرف الخلفي**: `InstitutionSetting` (AggregateRoot + Result + DomainErrors) مع `InstitutionSettingsController` — نفس النمط يُقلَّد لكيان التسميات. الهجرات: هجرة أولية واحدة `20261115073415_InitialCreate`؛ أي جدول جديد يضاف بهجرة جديدة عادية.
- **التدقيق**: `TamperEvidentAuditInterceptor` على مستوى SaveChanges — تعديلات التسميات ستُسجَّل عبره (يجب التحقق من شموليته للكيان الجديد في المرحلة 0).
- **الصلاحيات**: 24 رمزاً في `Domain/Common/Permissions.cs` (مصدر الحقيقة) و`shared/config/permissions.ts` (يجب أن يتطابقا). لا يوجد رمز مخصص لإعدادات العرض/التسميات؛ «إعدادات المؤسسة» محمية بـ `DEPARTMENT_MANAGE`.
- **بوابات الواجهة**: `npm run lint` + `npm run build` (`tsc -b`). لا ماضٍ اختباري للواجهة. بوابات الخلفية: `dotnet build` + `dotnet test` (يجب إيقاف API التطوير على 5028 أولاً لأنه يقفل bin — وإذا كان جلسة تصحيح VS نسأل المستخدم).
- **قواعد المشروع الملزمة** (AGENTS.md): FSD صارم (imports للأسفل فقط، لا deep imports)، القراءات في `entities` والكتابات في `features`، README لكل مجلد يتحدّث بنفس الدور مع Change Log، `knowledge.md` يتحدّث عند تغيّر endpoints/صلاحيات، عربي/RTL أولاً + فاتح/داكن، لا ألوان hex في المكونات.

---

## 2. الفكرة والقرارات المعمارية

### 2.1 قاموس مفاتيح مع افتراضيات مضمّنة + تجاوزات على السيرفر

- كل نص واجهة يُستبدل بمفتاح مستقر مثل `nav.documents` أو `documents.table.column.number` أو `common.save`.
- **الافتراضيات تبقى في الكود** (كتالوج مكتوب TypeScript) — التطبيق يعمل كاملاً بدون سيرفر أو بدون أي تجاوز. السيرفر يخزّن **فقط ما عُدّل**.
- الكيان الخلفي `UiLabelOverride`: `LabelId` (Guid، مفتاح أساسي — متطلب التدقيق)، `Key` (نص فريد ≤150)، `Value` (نص ≤200)، `UpdatedAt`، `UpdatedByUserId`. جدول `UI_LABEL_OVERRIDES` بفهرس فريد `UX_UI_LABEL_OVERRIDES_KEY`. لا seed — الجدول الفارغ يعني «كل شيء افتراضي». *(تعديل عن المسودة: المفتاح الأساسي Guid وليس Key، لأن `TamperEvidentAuditInterceptor` يشترط مفتاحاً Guid لتسجيل التدقيق.)*
- **مفاتيح مشتركة** (`common.save`, `common.cancel`, `common.delete`, …): تعديل واحد يسري على كل الأزرار في التطبيق — وهذه ميزة مقصودة (توحيد المصطلح)، وتُوثَّق للمستخدم داخل وضع التعديل.

### 2.2 اصطلاح المفاتيح والكتالوج

- الصيغة: `<area>.<surface>.<element>` بأحرف لاتينية صغيرة وشرطات، مثال:
  - `nav.group.correspondence`, `nav.documents`
  - `documents.page.title`, `documents.table.column.subject`, `documents.create.submit`
  - `common.save`, `common.cancel`, `common.search`
- الكتالوج في `shared/config/labels/` — ملف لكل منطقة (`common.ts`, `nav.ts`, `documents.ts`, …) يعيد تصديرها `index.ts` ككائن واحد مجمّد `LABEL_DEFAULTS` مع نوع `LabelKey` مشتق منه (type-safety: مفتاح غير موجود = خطأ بناء).
- **الplaceholders**: النصوص الديناميكية تُكتب `"هل تريد حذف {name}؟"` وتُستدعى `t('documents.deleteConfirm', { name })`. الاستبدال بسيط وآمن (لا HTML). أي تجاوز يجب أن يحوي نفس مجموعة الـ placeholders الموجودة في الافتراضي — يُتحقق منه في الواجهة قبل الحفظ.

### 2.3 طبقة الوصول في الواجهة: `t()` + `<Label>`

- `shared/lib/labels.ts`: متجر (zustand على غرار `useSessionStore`) يحوي `overrides: Record<string,string>` + `version` + `ready`. الدوال:
  - `t(key, params?)` → النص المعدّل إن وجد وإلا الافتراضي (مع الاستبدال). تُستخدم للنصوص داخل الخصائص (toast، aria-label، placeholder، رسائل الخطأ في `errorMessages.ts`).
  - `useLabel(key)` → خطّاف يعيد النص ويتفاعل مع تغيّر المتجر (لإعادة الرسم الفورية عند الحفظ).
- `shared/ui/label.tsx`: مكوّن `<Label k="…">` يرسم `<span>` بالنص. **هو نقطة إلصاق وضع التعديل**: عندما يكون الوضع مفعّلاً يضيف إطاراً خفيفاً عند hover، والنقر يبدّل الـ span بـ input في المكان نفسه (حفظ/إلغاء/استعادة الافتراضي) — **بدون أي حزمة جديدة** (لا popover؛ التبديل الموضعي أبسط وأخف، ويحقق «input مكان الزر» حرفياً).
- النصوص داخل خصائص المكونات الموحّدة تُمرَّر كمفاتيح حيث يلزم:
  - `data-table.tsx`: تعريف العمود يقبل `headerKey` بجانب `header` (الأولوية للمفتاح).
  - `form-field.tsx`: `labelKey` بجانب `label`.
  - `page-header.tsx`: `titleKey` / `descriptionKey`.
  - `shell-nav.ts`: `labelKey` لكل مجموعة وعنصر، والعارض يقرأ عبر `useLabel`.
  - الأزرار: يُغلَّف النص بـ `<Label>` في موقع الاستدعاء (لا تغيير على `button.tsx` نفسه).
- **تغطية وضع التعديل**: كل نص مرئي عبر `<Label>` أو عبر الخصائص المفتاحية أعلاه يكون قابلاً للتعديل مكانه. النصوص المحسوبة داخل `t()` فقط (مثل رسائل toast عابرة) تُعدَّل لاحقاً من صفحة مركزية إن أُضيفت (خارج النطاق، §6).

### 2.4 الجلب والتخزين المؤقت وشاشة الإقلاع

- `GET /api/ui-labels` (أي مستخدم مصادَق) → `{ version, overrides: { key: value } }`. الـ `version` = ticks لأحدث `UpdatedAt` (أو 0 عند الفراغ) — يتغيّر عند أي حفظ/حذف.
- الواجهة تخزّن النسخة في `localStorage` تحت `diwan.uiLabels.v1` (مع defensive parsing مثل تفضيلات الجداول).
- **الإقلاع**:
  - توجد نسخة مخزّنة → تُطبَّق فوراً (لا شاشة انتظار)، ويُعاد الجلب بالخلفية (stale-while-revalidate)؛ إن تغيّر الـ version تُحدَّث الواجهة بهدوء.
  - لا نسخة (أول دخول على هذا المتصفح) → **شاشة إقلاع مرتبة** (شعار المؤسسة + spinner + سطر «جارٍ تجهيز الواجهة…») تحجب التطبيق حتى يكتمل جلب التسميات و`/auth/me` معاً بالتوازي.
  - فشل الجلب → التطبيق يقلع بالافتراضيات + toast تحذيري غير حاجب (التسميات ليست حاجزاً وظيفياً).
- شاشة الإقلاع مكوّن في `app/providers` (بوابة `LabelsBootGate` تلفّ `AuthSessionLoader`/الموجّه)، تحترم السمة المحسومة مبكراً في `index.html` فلا وميض أبيض.

### 2.5 وضع التعديل

- الحالة في `shared/lib/labelEdit.ts` على نمط `tutorial.ts` تماماً (`diwan.labelEdit` في localStorage + سمة `data-label-edit` على `<html>` + `useLabelEdit()`).
- المفتاح يظهر في قائمة صدفة الـ shell **فقط لمن يملك الصلاحية** (نفس نمط «إيقاف الحركات»)، مع شريط علوي صغير ثابت أثناء التفعيل («وضع تعديل التسميات مفعّل — انقر أي نص لتعديله» + زر إنهاء) حتى لا يُنسى مفعّلاً.
- الحفظ: `PUT /api/ui-labels/{key}` body `{ value }` (upsert). الاستعادة: `DELETE /api/ui-labels/{key}`. كتابات محمية بالصلاحية، وقراءات للجميع.
- بعد الحفظ: تحديث المتجر فوراً ( optimistic ثم تثبيت من الرد) + invalidate لمفتاح استعلام التسميات + toast نجاح. كل المكونات المشتركة تعيد الرسم تلقائياً لأن النص يأتي من المتجر.
- التحقق قبل الحفظ: غير فارغ، ≤ 200 حرف، نفس placeholders الافتراضي، ولا يحوي وسوم HTML. السيرفر يتحقق من الطول وصيغة المفتاح أيضاً (حدود النظام).

### 2.6 الصلاحية — محسوم: حساب `superadmin` حصراً

الكتابة والاستعادة محميتان بسياسة `UI_LABELS_MANAGE` (`Server/Authorization/UiLabelPolicies.cs`) تشترط **دور `SUPER_ADMIN` واسم الحساب `superadmin` معاً** (ثوابت `SuperAdminSeeder`). السبب: دور `SUPER_ADMIN` وحده يمكن منحه لحسابات أخرى عبر `USER_MANAGE`، وربط السياسة باسم الحساب يحقق «فقط لحساب superadmin» حرفياً دون رمز صلاحية جديد قابل للمنح. القراءة (`GET`) لأي مستخدم مصادَق.
**أثر على الواجهة (المرحلة 2):** مفتاح وضع التعديل يظهر فقط عندما `user.username === 'superadmin'` (من `/api/auth/me`) — وليس عبر `hasPermission`.

### 2.7 حدود النطاق (ما لن نفعله)

- نصوص السيرفر (إشعارات، تدقيق، ProblemDetails) تبقى كما هي؛ `errorMessages.ts` (ترجمة أكواد الخطأ للعربية في الواجهة) **تُرحَّل إلى `t()`** فهي نص واجهة، لكنها ليست قابلة للتعديل Inline (تُعدَّل من الكتالوج فقط حالياً).
- أسماء البيانات (أنواع الوثائق، الأقسام، الأشخاص، الجداول المرجعية) قابلة للتعديل أصلاً عبر شاشاتها — ليست جزءاً من القاموس.
- لا صفحة مركزية لإدارة التسميات في هذه النسخة (وضع Inline فقط، حسب اختيار المستخدم).
- لا تعدد لغات — القاموس عربي فقط، لكن البنية (مفتاح → قيمة) لا تمنع إضافة لغات لاحقاً.

---

## 3. المراحل

> كل مرحلة تنتهي ببواباتها ثم **نتوقف لمراجعة المستخدم** قبل التالية (نمط التسليم المعتمد).

### المرحلة 0 — الطرف الخلفي: الكيان + API + اختبارات ✅ منفَّذة (2026-11-17)

**المنفَّذ فعلياً:**
1. `Domain/Entities/Ui/UiLabelOverride.cs` — AggregateRoot<Guid> مع `Create`/`UpdateValue`/`NormalizeKey` (تطبيع lowercase + صيغة مقاطع `^[a-z0-9]+([.\-][a-z0-9]+)*$`، حد 150/200).
2. `DomainErrors.UiLabel` (`KeyInvalid`, `ValueEmpty`, `ValueTooLong`).
3. `IUiLabelOverrideRepository` + `UiLabelOverrideRepository` + تسجيل DI.
4. `UiConfigurations.cs` (`UI_LABEL_OVERRIDES` + فهرس فريد على Key) + DbSet + هجرة `20261117093850_AddUiLabelOverrides`.
5. `Application/Features/Ui/UiLabels/`: `GetUiLabelOverridesQuery`، `SetUiLabelOverrideCommand` (upsert يعيد المجموعة كاملة + version)، `ResetUiLabelOverrideCommand` (idempotent)، `SetUiLabelOverrideCommandValidator`، `UiLabelOverridesResponse`.
6. `Server/Controllers/UiLabelsController.cs` + `Contracts/Ui/SetUiLabelOverrideRequest.cs` + سياسة `UI_LABELS_MANAGE` في `AuthenticationExtensions` (دور SUPER_ADMIN + اسم الحساب superadmin).
7. التدقيق: `AuditEntityType.UiLabelOverrideId = 19` + صف الـ seed + خريطة `("ORGANIZATION", 19)` في `TamperEvidentAuditInterceptor`.
8. 34 اختباراً جديداً: `UiLabelOverrideTests` (29: دومين + handlers + validator) و`UiLabelPolicyTests` (5: السياسة عبر composition root الحقيقي — دور SUPER_ADMIN على حساب آخر يُرفض) + `FakeUiLabelOverrideRepository`، وتحديث عدّاد أنواع التدقيق في `SeedingPipelineTests` (18→19).

**نتائج البوابات:** `dotnet build` ✅ · `dotnet test` **1064/1064** ✅ (يشمل `OpenApiDocumentationTests` الذي يفحص توثيق كل endpoint). الهجرة نظيفة (جدول واحد فقط). التوثيق: READMEs لكل طبقة + `knowledge.md` محدّثة بنفس الدور.
**لم يُنفَّذ بعد:** اختبار يدوي عبر HTTP حي (يتطلب تشغيل السيرفر وقاعدة بيانات) — يُغطى بالتحقق المتصفح في المرحلتين 1–2.

### المرحلة 1 — أساس الواجهة: الكتالوج + المتجر + الإقلاع (بدون وضع تعديل بعد) ✅ منفَّذة (2026-11-17)

1. `shared/config/labels/`: البدء بـ `common.ts` + `nav.ts` فقط (باقي المناطق تُضاف مع مراحل الترحيل). نوع `LabelKey` مشتق، تجميع في `index.ts`.
2. `shared/lib/labels.ts`: المتجر + `t()` + `useLabel()` + قراءة/كتابة `diwan.uiLabels.v1` + تطبيق التجاوزات.
3. `entities/ui-label/`: `api/` (GET) + `model/queries.ts` (مفتاح استعلام + `useUiLabelsQuery`) + types.
4. `app/providers/LabelsBootGate.tsx` + شاشة الإقلاع (شعار + spinner، فاتح/داكن، RTL/LTR) + دمجها في `AppProviders` بحيث تعمل بالتوازي مع `AuthSessionLoader`.
5. `shared/ui/label.tsx`: `<Label k>` بالعرض العادي فقط (لا تعديل بعد).
6. ترحيل **الـ sidebar كاملاً** كنموذج إثبات: `shell-nav.ts` → `labelKey`، والعارض عبر `useLabel`.

**المنفَّذ فعلياً:**
1. `shared/config/labels/`: `common.ts` (7 مفاتيح chrome/أفعال يستخدمها الـ sidebar) + `nav.ts` (5 مجموعات + 17 عنصراً + tagline/aria للقائمة) → `LABEL_DEFAULTS` مجمّد و`LabelKey = keyof typeof LABEL_DEFAULTS` عبر `index.ts`، وتصديرهما من برميل `shared/config`.
2. `shared/lib/labels.ts`: متجر zustand يُبذر من cache `diwan.uiLabels.v1` لحظة الإنشاء (أول رسم بلا وميض شاشة انتظار)، يحوي `overrides`/`version`/`ready` + `applySnapshot` (يكتب الـ cache)/`markReady`. `t(key, params?)` غير تفاعلي للخصائص/toast/aria، `useLabel(key, params?)` تفاعلي، و`resolveLabel(key, overrides, params?)` نقيّة. الاستبدال: تجاوز السيرفر ثم الافتراضي، مع interpolation لـ `{name}`. parsing دفاعي للـ cache.
3. `entities/ui-label/`: `UiLabelOverridesResponse` + `uiLabelApi.get` (`GET /ui-labels`) + `uiLabelKeys` + `useUiLabelsQuery` (staleTime 5د، بلا retry، `enabled` اختياري) + برميل + README.
4. `app/providers/LabelsBootGate.tsx`: شاشة إقلاع (`BrandMark` + `BrandName` + spinner + «جارٍ تجهيز الواجهة…»، `role="status"`)؛ يكشف الموجّه عند `!hasToken || ready`. الجلب مفعّل عند وجود token فقط؛ النجاح → `applySnapshot`، الفشل → `markReady` + toast تحذيري غير حاجب (فقط مع جلسة). رُكّب داخل `AuthSessionLoader` في `AppProviders` فيعمل جلب التسميات بالتوازي مع `/auth/me` (و`ProtectedRoute` يعرض spinner خاصته أثناء hydrate الجلسة). لا token → شاشة الدخول تقلع بالافتراضيات بلا جلب.
5. `shared/ui/label.tsx`: `<Label k params? className?>` يرسم `<span>` عبر `useLabel` (نقطة إلصاق وضع التعديل في المرحلة 2).
6. ترحيل الـ sidebar: `shell-nav.ts` استبدل `label` بـ `labelKey: LabelKey` لكل مجموعة/عنصر؛ `AppSidebar` يرسم النص المرئي عبر `<Label k>` ويحلّ الخصائص النصية (aria/title، قائمة السياق، tagline، شارة «قريبًا»، toasts النسخ) عبر `resolveLabel(key, overrides)` مشترك بـ `useLabelsStore` (يعيد الرسم في الوضعين المطوي/الموسّع)؛ عنوان المسار في `AppShell` يحلّ `findNavItem(...).labelKey` بنفس الطريقة.

**نتائج البوابات:** `npm run lint` ✅ (يشمل `check-guide-ids`) · `npm run build` ✅ (`tsc -b` + vite، تحذير حجم الـ chunks موجود مسبقاً). التوثيق: READMEs لكل مجلد touched + سطرا Change Log، و`knowledge.md` (قسم UI labels) و`labels/README.md` و`ui-label/README.md` محدّثة بنفس الدور.
**لم يُنفَّذ بعد:** التحقق المتصفح الفعلي (يتطلب دخولاً يدوياً من المستخدم): أول دخول بعد مسح localStorage تظهر شاشة الإقلاع ثم الـ sidebar؛ دخول ثانٍ فوري؛ PUT تجاوز يدوي ثم تحديث يظهر الاسم الجديد بالقائمة والـ header؛ فشل الشبكة يقلع بالافتراضيات.

### المرحلة 2 — وضع التعديل المباشر ✅ منفَّذة (2026-11-17)

1. `shared/lib/labelEdit.ts` (نمط tutorial.ts) + سمة `data-label-edit`.
2. `features/ui-label-manage/`: `api/` (PUT/DELETE + Zod) + `model/` (مخططات التحقق: طول، placeholders) + `ui/LabelEditor.tsx` (input موضعي: حفظ/إلغاء/استعادة، Enter يحفظ، Esc يلغي، focus محاصر).
3. تفعيل `<Label>` لوضع التعديل (hover outline + نقر → محرّر) — التنسيق عبر متغيرات CSS فقط، يعمل فاتح/داكن وRTL/LTR.
4. الشريط العلوي للوضع + مفتاح التفعيل في قائمة الصدفة (يظهر فقط لحساب `superadmin` — فحص `username` من الجلسة، يغيب عن DOM لغيره).
5. تمديد `data-table.tsx` (`headerKey`) و`form-field.tsx` (`labelKey`) و`page-header.tsx` (`titleKey`) — هذه المرحلة **تضيف Capability فقط**؛ الترحيل الفعلي في المراحل 3–4.

**التحقق:** lint + build. متصفح بأدمن: تفعيل الوضع → تعديل اسم عنصر sidebar → ينعكس فوراً بكل الصفحات وبالـ header؛ استعادة الافتراضي؛ حفظ نص يحوي placeholder ناقص يُرفض برسالة عربية؛ مستخدم بلا صلاحية لا يرى المفتاح ولا يستطيع PUT (403). التحقق من عدم تسرّب وضع التعديل للمستخدم العادي.

**المنفَّذ فعلياً:**
1. `shared/lib/labelEdit.ts`: `getLabelEditMode`/`applyLabelEditMode`/`setLabelEditMode`/`useLabelEdit` على `diwan.labelEdit` + `html[data-label-edit]` (نمط tutorial.ts حرفياً، فقط `on` يُخزَّن)، حسم مبكر في `index.html`، وتصدير من برميل `shared/lib`.
2. `features/ui-label-manage/`: `model/types.ts` (`SetUiLabelOverrideRequest`/`Variables`)، `model/schemas.ts` (`UI_LABEL_MAX_LENGTH=200` + `uiLabelValueSchema(defaultText)`: غير فارغ، ≤200، لا وسوم HTML، وتطابق placeholders الافتراضي)، `api/uiLabelMutations.ts` (`useSetUiLabelMutation`/`useResetUiLabelMutation`: تطبيق تفاؤلي → تثبيت من الرد الكامل → rollback عند الفشل + invalidate لـ `uiLabelKeys.overrides()`)، `ui/LabelEditor.tsx` (input + حفظ/إلغاء/استعادة، Enter يحفظ وEsc يلغي، focus محاصر، منع تنقّل الرابط الأب، أخطاء عربية + toast نجاح)، `ui/UiLabelEditProvider.tsx`، برميل + README.
3. الجسر بين shared وfeatures عبر `shared/ui/label-edit-context.ts` (`LabelEditContext` + `useLabelEditorRenderer` + الأنواع) — يزوّد التطبيق الـ renderer وتستهلكه `<Label>` بلا استيراد صاعد. `<Label>` صار قابلاً للنقر/Enter للتحرير عند تفعيل الوضع (إطار hover من `html[data-label-edit='on'] .diwan-label-editable` في `app/styles/index.css` عبر المتغيرات فقط). ركّب `AppProviders` الـ `UiLabelEditProvider` داخل `LabelsBootGate`.
4. `AppShell`: مفتاح «تفعيل/إيقاف تعديل التسميات» في قائمة الصدفة + شريط علوي لاصق أثناء التفعيل مع زر «إنهاء الوضع» — كلاهما فقط عندما `user.username === 'superadmin'` (يغيب عن DOM لغيره). `LabelsBootGate` يفرض إطفاء الوضع لأي جلسة غير superadmin (دفاع إضافي ضد تسرّب تفضيل المتصفح)، والسيرفر 403 هو الحارس الأخير.
5. Capability فقط: `data-table` عمود `headerKey` (يحَلّ عبر المتجر للرأس/قائمة الأعمدة/aria/التصدير)، `form-field` `labelKey` و`page-header` `titleKey`/`descriptionKey` (ترسم عبر `<Label>` فتكون قابلة للتحرير مكانه). أُضيفت أكواد `UiLabel.*` إلى `errorMessages.ts`.

**نتائج البوابات:** `npm run lint` ✅ (Guide catalogue OK: 115/20/118) · `npm run build` ✅ (`tsc -b` + vite، تحذير حجم الـ chunks موجود مسبقاً). التوثيق: READMEs لكل مجلد touched + Change Log، و`knowledge.md` (قسم UI labels) محدّثة بنفس الدور.
**لم يُنفَّذ بعد:** التحقق المتصفح الفعلي (يتطلب دخولاً يدوياً من المستخدم): تفعيل الوضع كأدمن → تعديل تسمية sidebar وانعكاسها فوراً وبالـ header؛ الاستعادة؛ رفض placeholder ناقص برسالة عربية؛ مستخدم عادي لا يرى المفتاح ولا الشريط ويأخذ 403 على PUT؛ عدم تسرّب الوضع بعد تبديل الحساب.

### المرحلة 3 — ترحيل النصوص: الدفعة أ (المشتركات + الهيكل) ✅ منفَّذة (2026-11-17)

النطاق: `common.*` (حفظ/إلغاء/حذف/بحث/تحديث/إغلاق/تأكيد…)، عناوين وأوصاف كل الصفحات عبر `page-header`، أدوات `data-table` الموحّدة (بحث، الأعمدة، صفوف في الصفحة، تصدير)، رسائل `confirm-dialog` العامة، قائمة الصدفة وحوار الإعدادات، `errorMessages.ts` → `t()`.
كل ملف يُلمس يُحدَّث README خاصته بنفس الدور (قاعدة AGENTS.md).

**التحقق:** lint + build + جولة متصفح على 3 صفحات مختلفة (فاتح/داكن): كل نص مشترك يعدّل من مكان واحد ويسري في كل Places؛ لا نص عربي متبقٍ خارج الكتالوج في الملفات المرحلة (فحص grep).

**ما نُفّذ (2026-11-17):**
- الكتالوج: توسيع `common.ts` (أفعال عامة، أدوات الجدول، قائمة الصدفة + الشريط، قائمة الحساب، حوار الإعدادات، احتياطيات `common.error.*`)؛ إنشاء `pages.ts` (عناوين/أوصاف 18 صفحة ثابتة) و`errors.ts` (كتالوج الأخطاء الكامل ~358 رمزًا بمفاتيح `error.<code-lowercased>`)؛ دمجها في `LABEL_DEFAULTS`.
- `errorMessages.ts`: أُعيدت كتابته بلا نص عربي — `CODE_MESSAGES: Record<string, LabelKey>` يربط كل رمز بمفتاحه، و`toArabicErrorMessage` يحل عبر `t()`؛ احتياطيات الحالة/عام/شبكة عبر `common.error.*`. وُلّد الكتالوج والخريطة بسكربت Node لضمان التطابق (حُذف السكربت بعد التوليد).
- المستهلكات: `data-table.tsx` + `data-table-export.tsx` + `confirm-dialog.tsx` → `t()`/`<Label>` (مع إبقاء `dir="ltr"` لأرقام الترقيم)؛ `AppShell` + `AppHeader` + `SettingsDialog` → `t()`؛ و18 صفحة → `titleKey`/`descriptionKey`. العناوين الديناميكية (موضوع الوثيقة، اسم الشخص) ووصف الوثائق المركّب من أعلام الميزات بقيت في الكود.
- الوثائق: READMEs المجلدات الـ18 + `shared/config` و`labels` و`shared/ui` و`shared/api` و`widgets/app-shell` + `knowledge.md` + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (Guide catalogue OK: 115/20/118) + `npm run build` ✅ (tsc + vite؛ تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep: لا عربية متبقية في ملفات الجدول المرحلة (تعليق توثيقي واحد فقط).
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم) على 3 صفحات فاتح/داكن للتحقق من السريان والاستعادة.

### المرحلة 4 — ترحيل النصوص: دفعات المناطق (كل دفعة مراجعة مستقلة)

تقسيم مقترح حسب مناطق الـ sidebar، كل دفعة = منطقة كاملة (صفحات + widgets + features التابعة):
- **ب1: المراسلات** — documents, movements, decisions, my-registers, persons, archive (الأكبر؛ تُجزّأ داخلياً إلى documents أولاً ثم الباقي إذا لزم).
- **ب2: التنظيم** — departments, institution-settings.
- **ب3: إدارة الوصول** — users, roles, employees.
- **ب4: الرقابة والتدقيق** — numbering, document-schemas, lookups, audit, system-errors.
- **ب5: المتبقي** — dashboard, notifications, auth (شاشة الدخول)، والطباعة/التصدير إن حوت نصوصاً ثابتة.

لكل دفعة: إضافة ملف كتالوج المنطقة، استبدال النصوص، تحديث READMEs، جولة متصفح للمنطقة (تعديل 3 تسميات مختلفة على الأقل والتحقق من سريانها + الاستعادة).

#### ب1 — وحدة documents الأولى (شاشة قائمة المراسلات) ✅ منفَّذة (2026-11-18)

أول وحدة قابلة للمراجعة داخل ب1 (الأكبر)، اقتصرت على شاشة القائمة دون صفحة تفاصيل الوثيقة:

- **الكتالوج:** `shared/config/labels/documents.ts` (`DOCUMENT_LABELS`، نطاق `documents.*`) — تبويبا الصفحة، جدول السجل (رؤوس الأعمدة، المرشحات، قوائم الصف، toasts النسخ، حالات الفراغ)، `RegisterNumberFilter`، وحوار الإنشاء (الأقسام، الحقول، البطاقات، toasts، رسائل Zod). دُمج في `LABEL_DEFAULTS`.
- **الجدول:** `DataTableColumn.header` صار اختياريًا (`header?: string`) ليُعرَّف العمود بـ `headerKey` وحده؛ `headerOf` يرتد إلى `header ?? ''`. رؤوس الأعمدة الثابتة عبر `headerKey` (تُعاد رسمها عند التعديل عبر المتجر)، وخيارات «الكل» الثلاثة عبر `useLabel` (سريان فوري)، والأزرار/إجراءات الفراغ عبر `<Label>` (قابلة للتحرير مكانه)، والقوائم/toasts/aria/placeholder/caption/hالات الفراغ عبر `t()`. رؤوس أعمدة الحقول الديناميكية بقيت `field.labelAr`.
- **الصفحة:** تبويبا `registry`/`organizations` عبر `<Label>`، وaria للتبويبات عبر `t()`. وصف الصفحة المركّب من أعلام الميزات بقي في الكود (استثناء المرحلة 3).
- **حوار الإنشاء:** `FormSection`/`ChoiceCard` صارا يقبلان `titleKey`/`hintKey` (يرسمان عبر `<Label>`)، و`FormField`s عبر `labelKey`، والوصف المركّب من الأعلام يُركَّب من أجزاء `t()`، ورسائل Zod عبر `t('documents.create.validation.*')`. placeholder المثال `1447/2026` بقي حرفيًا.
- **الوثائق:** READMEs المجلدات الستة (`labels`، `shared/config`، `shared/ui`، `pages/documents`، `widgets/document-registry-table`، `features/document-create`) + `knowledge.md` (قسم UI labels) + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (Guide catalogue OK: 115/20/118) + `npm run build` ✅ (tsc + vite؛ تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep: لا نص عربي حرفي متبقٍ في ملفات الوحدة عدا وصف الصفحة المركّب (استثناء موثّق).
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل 3 تسميات مختلفة على الأقل (رأس عمود، زر، تسمية حقل) والتحقق من سريانها في القائمة والحوار + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من ب1:** decisions, my-registers, persons, archive (تنتظر موافقة صريحة على الوحدة التالية).

#### ب1 — وحدة movements (منطقة الحركات/التوجيه) ✅ منفَّذة (2026-11-18)

ثاني وحدة قابلة للمراجعة داخل ب1، غطّت منطقة الحركات كاملة (الصفحة + الصندوق + feature التوجيه + قوالب ملاحظات الـ entity):

- **الكتالوج:** `shared/config/labels/movements.ts` (`MOVEMENT_LABELS`، نطاق `movements.*`) — تبويبا الصفحة، صندوق الوارد/الصادر (رؤوس الأعمدة، المرشحات، قوائم الصف، toasts النسخ، خلايا الخطوة/المسار/متأخر، حالات الفراغ)، تسميات الإجراءات (`movements.action.*`)، كل حوارات الإجراءات (فتح/إرسال/استلام/إكمال/إغلاق/رفض/إرجاع/إعادة توجيه/إلغاء — العناوين، الأوصاف، الحقول، التلميحات، toasts، رسائل Zod)، تلميح مسار الحفظ، منتقي المكلَّف، وقوالب ملاحظات الخط الزمني. دُمج في `LABEL_DEFAULTS`.
- **خريطة الإجراءات:** `MOVEMENT_ACTION_LABELS` (قيم نصية) صارت `MOVEMENT_ACTION_LABEL_KEYS` (`Record<MovementAction, LabelKey>` → `movements.action.*`)؛ تُحلّ عبر `t()` في قائمة صف الصندوق وعناوين الحوارات، وعبر `<Label>` في `MovementActionButtons`. حُدّثت كل المستهلكات (الصندوق، الحوارات، الأزرار، الـ barrel).
- **الصندوق:** رؤوس الأعمدة عبر `headerKey`، وخيارا «الكل» عبر `useLabel`، وخلايا الخطوة/المسار/متأخر والأزرار/إجراءات الفراغ عبر `<Label>` (مع إبقاء spans الأرقام `dir="ltr"`)، والقوائم/toasts/aria/caption/البحث/حالات الفراغ عبر `t()`.
- **الحوارات:** العناوين/الأوصاف/التلميحات/toasts عبر `t()`، والحقول عبر `FormField labelKey`، والأزرار عبر `<Label>`؛ رسائل Zod عبر `t('movements.validation.*')`؛ خيار «بدون إسناد» عبر `useLabel`. placeholder المثال `FIN-001/2026` بقي حرفيًا.
- **الـ entity:** `translateMovementNote` في `entities/movement/model/rules.ts` يحلّ قالبَيه عبر `t('movements.note.routed-to-central' / '.rejected', { detail })`.
- **الوثائق:** READMEs المجلدات الستة (`labels`، `shared/config`، `pages/movements`، `widgets/movement-inbox`، `features/movement-routing`، `entities/movement`) + `knowledge.md` (قسم UI labels) + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (Guide catalogue OK: 115/20/118) + `npm run build` ✅ (tsc + vite؛ تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep: لا نص عربي حرفي متبقٍ في ملفات الوحدة عدا التعليقات التوثيقية وplaceholder المثال (استثناء موثّق).
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل 3 تسميات مختلفة على الأقل (رأس عمود، زر إجراء، تسمية حقل في حوار) والتحقق من سريانها في الصندوق والحوارات + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من ب1:** decisions, my-registers, persons, archive (تنتظر موافقة صريحة على الوحدة التالية).

#### ب1 — وحدة decisions (أ): السطح القرائي ✅ منفَّذة (2026-11-18)

ثالث وحدة قابلة للمراجعة داخل ب1. منطقة القرارات كبيرة (لوحة الوثيقة + 7 حوارات + جدولان + خرائط تسميات)، فجُزّئت داخليًا كما تنص الخطة؛ هذه الوحدة تغطي **السطح القرائي فقط** (الصفحة + الجدولان + خرائط التسميات المشتركة + شارة الحالة)، والحوارات ولوحة الوثيقة تكون الوحدة التالية (ب):

- **الكتالوج:** `shared/config/labels/decisions.ts` (`DECISION_LABELS`، نطاق `decisions.*`) — تبويبا الصفحة، خرائط الأثر/الحالة/نتيجة الاعتراض (مع احتياطيات `{id}`)، جدول سجل القرارات (رؤوس الأعمدة، المرشحات، قوائم الصف، حالات الفراغ)، وجدول أنواع القرارات (الرؤوس، خلايا قابل/غير قابل ومفعّل/موقوف، القوائم، toasts، المرشح، حالات الفراغ). دُمج في `LABEL_DEFAULTS`.
- **خرائط التسميات المشتركة:** `shared/config/decisionLabels.ts` — الخرائط الثلاث صارت `Record<number, LabelKey>` (`decisions.effect.*`/`status.*`/`appeal-outcome.*`) والدوال تحلّ عبر `t()` مع احتياطي `{id}`؛ **التواقيع بلا تغيير** فتبقى كل المستهلكات (الجدولان، `DecisionStatusBadge`، لوحة الوثيقة، حوارا الإصدار/النوع) تعمل وترث إعادة التسمية. `isDecisionInForce` بقي فحص ID صرفًا. الاعتماد config→lib آمن (الدالة `t` مرفوعة ولا تُستدعى وقت تحميل الوحدة).
- **الصفحة:** `DecisionsPage` التبويبان عبر `<Label>` وaria عبر `t()`.
- **جدول السجل:** رؤوس الأعمدة عبر `headerKey`، وخيارات «الكل» الثلاثة عبر `useLabel`، وزر مسح المرشحات عبر `<Label>`، والقوائم/aria/caption/البحث/حالات الفراغ عبر `t()`؛ خليتا الأثر/الحالة تحلان عبر `decisionEffectLabel`/`DecisionStatusBadge`.
- **جدول الأنواع:** الرؤوس عبر `headerKey`، وخلايا قابل/غير قابل ومفعّل/موقوف وزر «نوع جديد» عبر `<Label>`، وخيارات مرشح الحالة وplaceholder عبر `useLabel`، وtoasts/القوائم/aria/caption/البحث/حالات الفراغ عبر `t()`.
- **شارة الحالة:** `DecisionStatusBadge` بلا تغيير كود (ترث الحل عبر `decisionStatusLabel`)، فالمجلد غير متأثر ولا README له.
- **الوثائق:** READMEs المجلدات الثلاثة (`labels`، `shared/config`، `pages/decisions`) + `knowledge.md` (قسم UI labels) + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (Guide catalogue OK: 115/20/118) + `npm run build` ✅ (tsc + vite؛ تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep: لا نص عربي حرفي متبقٍ في `pages/decisions/ui`.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية رأس عمود + تسمية خلية (مفعّل/موقوف) + تسمية أثر/حالة عبر الخرائط المشتركة والتحقق من سريانها في الجدولين وشارة الحالة + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من منطقة decisions:** الحوارات (`features/decision-manage` الستة + `features/decision-type-manage`) ولوحة الوثيقة (`widgets/document-decisions-panel`) — الوحدة (ب) التالية. ومن ب1 أيضًا: my-registers, persons, archive (تنتظر موافقة صريحة).

#### ب1 — وحدة decisions (ب): الحوارات + لوحة الوثيقة ✅ منفَّذة (2026-11-18)

رابع وحدة قابلة للمراجعة داخل ب1، تُكمل منطقة القرارات (الحوارات + لوحة الوثيقة). بأمر المستخدم «كمل لتخلص للاخر» تُنفَّذ الدفعات المتبقية تباعًا دون توقف بعد كل وحدة:

- **الكتالوج:** وُسّع `shared/config/labels/decisions.ts` بمفاتيح `decisions.issue/cancel/appeal/outcome/attach/join/type.*` (العناوين، الأوصاف، الحقول، التلميحات، toasts، رسائل Zod) و`decisions.next.*` و`decisions.panel.*` (البطاقة النافذة، نص التوحيد/الاقتراح، الأزرار، رؤوس الأعمدة، حالات الفراغ) و`decisions.decision-prefix`. منطقة القرارات صارت مُفهرسة بالكامل.
- **الحوارات الستة + منتقي الملف الرئيسي:** العناوين/الأوصاف/toasts/placeholder/aria عبر `t()`، والحقول عبر `FormField labelKey`، والأزرار عبر `<Label>` (إلغاء/حفظ يعيدان استخدام `common.action.*`)؛ رسائل Zod عبر `t()` وقت التحميل. خيارات نتيجة الاعتراض تعيد استخدام `decisions.appeal-outcome.*` عبر `useLabel`. خيار «الديوان المنشئ» عبر `useLabel`.
- **حوار نوع القرار:** الخيارات انتقلت إلى داخل المكوّن (`EFFECT_IDS` + `decisionEffectLabel`) لتُحلّ عبر `t()` عند كل رسم؛ تسمية checkbox عبر `t()`.
- **لوحة الوثيقة:** `NEXT_ACTION_LABEL` صار `NEXT_ACTION_LABEL_KEYS` (`Record<string, LabelKey>` → `decisions.next.*`)؛ عناوين البطاقات/الأزرار/نص التوحيد/الاقتراح/خلية «من الملف الرئيسي» عبر `<Label>`، والرؤوس عبر `headerKey`، والقوائم/caption/البحث/الفراغ عبر `t()`. الأثر/الحالة/الاعتراض ترث المحلّلات المشتركة.
- **الوثائق:** READMEs (`features/decision-manage`، `features/decision-type-manage`، `widgets/document-decisions-panel`، `labels`) + `knowledge.md` + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅. فحص grep: لا نص عربي حرفي متبقٍ في الميزات واللوحة.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية زر حوار + تسمية حقل + تسمية عنوان بطاقة والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من ب1:** persons, archive.

#### ب1 — وحدة my-registers (سجلاتي) ✅ منفَّذة (2026-11-18)

خامس وحدة داخل ب1، تغطي متصفّح سجلات القسم (`/my-registers`). الصفحة نفسها كانت مرحّلة في المرحلة 3 (`page.my-registers.*` عبر `PageHeader`)، فالترحيل هنا للـ widget:

- **الكتالوج:** أُضيف `shared/config/labels/my-registers.ts` (`MY_REGISTERS_LABELS`, ~48 مفتاحًا `my-registers.*`) — caption/البحث، خيارات مرشّح الحالة، منتقيات السجل/النموذج/الإصدار (لاحقة «موقوف»، «بلا بيانات نموذج»، تسمية الإصدار المركّبة، fallback «نوع {id}»)، الأعمدة، بادئات الرقم المرتبط، قوائم الصف، toasts النسخ، aria/placeholder للمرشحات، وحالات الفراغ المتدرّجة. دُمج في `LABEL_DEFAULTS`.
- **الـ widget:** `STATUS_OPTIONS` على مستوى الوحدة صار خيارات `useLabel` داخل المكوّن؛ لاحقة «موقوف» و«بلا بيانات نموذج» عبر `useLabel`؛ تسمية الإصدار تُركّب عبر `t()`؛ رؤوس الأعمدة عبر `headerKey`؛ خلية الرقم المرتبط وأزرار المسح عبر `<Label>`؛ caption/البحث/aria/placeholder/القوائم/toasts/الفراغ عبر `t()`؛ قائمة التحديث تعيد استخدام `table.refresh`. أعمدة حقول النموذج الديناميكية تبقى `field.labelAr`؛ `FieldFilterBar` تابع لمنطقة document-schema (ب4).
- **الوثائق:** READMEs (`widgets/my-registers-table`، `pages/my-registers`، `labels`، `shared/config`) + `knowledge.md` + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅. فحص grep: لا نص عربي حرفي متبقٍ في الـ widget.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية عمود + خيار مرشّح + حالة فراغ والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من ب1:** archive.

#### ب1 — وحدة persons (الأشخاص) ✅ منفَّذة (2026-11-18)

سادس وحدة داخل ب1، تغطي منطقة الأشخاص كاملة (القائمة + ملف الشخص بتبويبيه + حوارا الوثيقة + حقل الأسماء المشترك):

- **الكتالوج:** أُضيف `shared/config/labels/persons.ts` (`PERSONS_LABELS`, `persons.*`) — قائمة الأشخاص (caption/البحث/الأعمدة/قائمة الصف/الفراغ)، تبويبا ملف الشخص وروابط العودة، جدول وثائق الشخص (الأعمدة/المرشحات/الفراغ)، السجل الزمني (روابط الأحداث من/إلى/في/رُبط…/بواسطة، الفراغ، الترقيم)، حوارا إضافة الأشخاص وتغيير الصفة (العناوين/الأوصاف/الحقول/toasts/Zod، حقل الصفة المشترك `persons.field-role`/`persons.role-*`)، وحقل `PersonNamesInput` (placeholder/تفاصيل الاقتراح/خيار الجديد/تنبيه التكرار/aria الشرائح/شارة «جديد»/aria الإزالة). دُمج في `LABEL_DEFAULTS`.
- **السطوح:** `PersonsTable`/`PersonDocumentsTable`/`PersonTimeline` عبر `t()`/`headerKey`/`useLabel`/`<Label>`؛ «تغيير الحالة» مشترك `persons.status-change`؛ «إعادة المحاولة»/«تحديث البيانات» يعيدان استخدام `common.action.retry`/`table.refresh`. `PersonDetailsPage` التبويبان عبر `<Label>` وTABS صار `labelKey`. الحواران عبر `FormField labelKey` + `<Label>` (إلغاء/حفظ `common.action.*`) وZod عبر `t()`. `PersonNamesInput` (entity) عبر `t()`/`<Label>`؛ `normalizeName.ts` يبقى (regex وظيفي لا تسمية).
- **الوثائق:** READMEs (`widgets/persons-table`، `widgets/person-documents-table`، `widgets/person-timeline`، `pages/persons`، `features/document-person-manage`، `entities/person`، `labels`، `shared/config`) + `knowledge.md` + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅. فحص grep: لا نص عربي حرفي متبقٍ في أسطح المنطقة (عدا regex التطبيع).
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية تبويب + عمود + شارة «جديد» والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **لم يُنفَّذ بعد من ب1:** archive.

#### ب1 — وحدة archive (الأرشيف) ✅ منفَّذة (2026-11-18) — ب1 اكتمل

سابع وآخر وحدة داخل ب1، تغطي منطقة الأرشيف (الشبكة + شريط عمليات البحث المحفوظة + الحوارات الأربعة). بهذا **اكتمل ب1 (المراسلات)**: documents, movements, decisions, my-registers, persons, archive.

- **الكتالوج:** أُضيف `shared/config/labels/archive.ts` (`ARCHIVE_LABELS`, `archive.*`) — شبكة الوثائق المؤرشفة (caption/البحث/الأعمدة/قوائم الصف/toasts النسخ/المرشحات/الفراغ)، شريط عمليات البحث المحفوظة (aria الشرائح/التعديل/الحذف)، وحوارات الأرشفة/إلغاء الأرشفة/حفظ البحث/حذف البحث (العناوين/الأوصاف/الحقول/toasts/Zod). مشترك `archive.dismiss` («تراجع») و`archive.reload` («إعادة التحميل»)؛ الحفظ يعيد استخدام `common.action.save`. دُمج في `LABEL_DEFAULTS`.
- **السطوح:** `ArchiveSearchTable`/`SavedSearchesBar` عبر `t()`/`headerKey`/`useLabel`/`<Label>`؛ خيارات «كل الأنواع/المواقع/الأقسام» عبر `useLabel`. الحوارات الأربعة عبر `FormField labelKey` + `<Label>` + `t()` (ConfirmDialog حذف البحث عبر confirmLabel/cancelLabel). Zod عبر `t()`. `entities/archive` (criteria/badge) منطق/بيانات بلا تغيير؛ `FieldFilterBar` تابع لـ document-schema (ب4).
- **الوثائق:** READMEs (`widgets/archive-search-table`، `features/archive-manage`، `pages/archive`، `labels`، `shared/config`) + `knowledge.md` + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅. فحص grep: لا نص عربي حرفي متبقٍ في أسطح المنطقة.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية عمود + عنوان حوار + شريحة بحث محفوظ والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **التالي:** ب2 (التنظيم: departments + institution-settings).

#### ب2 — التنظيم (departments + institution-settings + external-organizations) ✅ منفَّذة (2026-11-18) — ب2 اكتمل

أول وحدة داخل ب2، تغطي منطقة التنظيم كاملة: شجرة/جدول/مخطط/إحصاء الأقسام + حوارات التفاصيل والموظفين، دفتر سجل القسم، لوحة السجلات + نموذج تعديل الزيارة، إعدادات المؤسسة + حقل الشعار، والجهات الخارجية (الجدول + النموذج). بأمر «كمل لتخلص للاخر» تُنفَّذ الدفعات تباعًا دون توقف بعد كل وحدة.

- **الكتالوج:** أُضيفت ثلاثة ملفات — `departments.ts` (`DEPARTMENTS_LABELS`, `department.*` ~180 مفتاحًا)، `institution-settings.ts` (`INSTITUTION_SETTINGS_LABELS`, `institution.*` ~60)، `external-organizations.ts` (`EXTERNAL_ORGANIZATIONS_LABELS`, `external-org.*` ~55)؛ دُمجت في `LABEL_DEFAULTS`.
- **السطوح:** الأعمدة عبر `headerKey`؛ خيارات المرشحات/الحالة الثابتة عبر `useLabel` (مع إضافة القيمة إلى deps الـ memo)؛ النصوص الظاهرة القابلة للنقر عبر `<Label>`؛ تسميات الحقول عبر `FormField labelKey`؛ القوائم/toasts/aria/placeholders/الفراغ/رسائل Zod عبر `t()`. `toMermaid` لاحقة «موقوف» عبر `t()`؛ `DepartmentBadge` عبر `<Label>`. الأسماء/الرموز/`descAr`/`field.labelAr` تبقى بيانات؛ أمثلة `Asia/Damascus`/`MOI-01`/الإنجليزية تبقى حرفية؛ regex التطبيع/الهاتف منطق بلا تغيير.
- **الوثائق:** READMEs (`widgets/department-hierarchy-tree`، `department-register-book`، `department-records-panel`، `external-organizations-table`؛ `features/department-manage`، `department-record-manage`، `institution-settings-manage`، `external-organization-manage`؛ `entities/department`؛ `labels`؛ `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في أسطح المنطقة عدا التعليقات التوثيقية وREADMEs.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية عمود قسم + عنوان قسم إعدادات + شارة «موقوف» والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **التالي:** ب3 (إدارة الوصول: users + roles + employees).

#### ب3 — إدارة الوصول (users + roles + employees) ✅ منفَّذة (2026-11-18) — ب3 اكتمل

تغطي منطقة إدارة الوصول كاملة: شبكات المستخدمين/الأدوار/الموظفين + كل حوارات الكتابة (نموذج المستخدم/الأدوار/الوصول/إعادة كلمة المرور، نموذج الدور/الصلاحيات، نموذج الموظف) + مخططات Zod الثلاثة.

- **الكتالوج:** أُضيف `access.ts` (`ACCESS_LABELS`, `user.*` / `role.*` / `employee.*` ~230 مفتاحًا)؛ دُمج في `LABEL_DEFAULTS`.
- **السطوح:** الأعمدة عبر `headerKey`؛ شارات الحالة/«أنت»/«مطلوب»/النظام/المخصص/عدد الصلاحيات عبر `t()`/`<Label>`؛ خيار «كل الأقسام» الثابت عبر `useLabel`؛ القوائم/نسخ/toasts/aria/placeholders/الفراغ/رسائل Zod عبر `t()`؛ أزرار الأدوات/الفراغ/الحوارات عبر `<Label>`؛ تسميات الحقول عبر `FormField labelKey`؛ حوارات التأكيد عبر `t({name})`. الأسماء/الرموز/`permission.nameAr`/تسميات الوحدات تبقى بيانات؛ أمثلة `ahmed.khalil`/`FINANCE_EDITOR`/الإنجليزية تبقى حرفية؛ `z.email().safeParse` منطق بلا تغيير.
- **الوثائق:** READMEs (`widgets/users-table`، `roles-table`، `employees-table`؛ `features/user-manage`، `role-manage`، `employee-manage`؛ `labels`؛ `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات `.ts/.tsx` المنطقة.
- **متبقٍ:** جولة المتصفح (دخول يدوي من المستخدم): تعديل تسمية عمود مستخدم/دور/موظف + عنوان حوار والتحقق من السريان + الاستعادة؛ فاتح/داكن.
- **التالي:** ب4 (الرقابة والتدقيق: numbering + document-schemas + lookups + audit + system-errors).

#### ب4 — الرقابة والتدقيق (numbering + document-schemas + lookups + audit + system-errors) ✅ منفَّذة (2026-11-18) — ب4 اكتمل

تغطي مناطق الرقابة الخمس: الترقيم (module 04)، مخططات الحقول الديناميكية (module 03)، القوائم المرجعية، التدقيق وسلامة السجل (module 09)، وأخطاء النظام (module 09).

- **الكتالوج:** أُضيفت `numbering.ts` (`NUMBERING_LABELS`, `numbering.*` ~166 مفتاحًا)، `document-schemas.ts` (`DOCUMENT_SCHEMAS_LABELS`, `schema.*` ~180)، `lookups.ts` (`LOOKUPS_LABELS`, `lookup.*`)، `audit.ts` (`AUDIT_LABELS`, `audit.*`)، `system-errors.ts` (`SYSTEM_ERRORS_LABELS`, `system-error.*`)؛ دُمجت في `LABEL_DEFAULTS`. ووُسّع `documents.ts` بمفاتيح الوثيقة المقيّدة (`documents.restricted.*`) وشارات حالة الرقم (`documents.number-status.*`).
- **الترقيم:** شبكة السجلات عبر `headerKey` + خيارات الحالة/النطاق/سياسة التصفير عبر خرائط مفاتيح + `resolveLabel`؛ بطاقة النطاق والحوار القرائي والحوارات الأربعة (إصدار/استبدال/إلغاء/تسجيل دائرة) ونموذج السجل عبر `t()`/`<Label>`/`FormField labelKey`؛ رسائل Zod عبر `t()`؛ رموز النمط `{YEAR}`/`{SEQ}`/`{SEQ:D6}` تبقى حرفية (لا تُعامل كـ placeholders).
- **المخططات:** `entities/document-schema` (شارات النوع/الحالة عبر `useLabel`، تنسيق القيم والتحقق والمرشحات عبر `t()`، عناصر الإدخال وشريط/حوار التصفية عبر `t()`/`resolveLabel`/`<Label>`)؛ `features/document-schema-manage` (نموذج الإعدادات والسلوك — نُقلت خيارات النوع/المصدر/القوائم المرجعية إلى داخل المكوّن مع اشتراك overrides وحل `lookupCatalogMeta.labelKey`، حوارات التأكيد، Zod)؛ محرر المخطط ولوحة حقول الوثيقة ونموذج تعديل القيم. `field.labelAr`/`option.labelAr`/نصوص الأقسام والمساعدة والـ placeholder ورسائل Regex تبقى بيانات؛ أمثلة `contract_number`/`key`/`^[0-9]{4}` تبقى حرفية.
- **القوائم المرجعية:** أسماء العرض الثابتة للكتالوجات/المجموعات عبر `lookupCatalogMeta` (`labelKey`/`groupKey`) + `resolveLabel` (مع سقوط إلى الـ slug عند `labelKey` null)؛ شبكة الإدخالات ونموذج الإضافة/إعادة التسمية عبر `headerKey`/`t()`/`<Label>`/Zod. قيم الإدخالات `descAr`/`LOOKUP_FALLBACKS` تبقى بيانات.
- **التدقيق:** شبكتا سجل التدقيق وتقارير السلامة عبر `headerKey` + خريطة `AUDIT_MODULES` (`audit.module.*`) + `resolveLabel`؛ حوارات نقطة التحقق/السلامة وZod عبر `t()`/`<Label>`؛ `descAr` لأنواع الإجراءات/الكيانات والأسماء والـ hashes تبقى بيانات.
- **أخطاء النظام:** خريطة التعداد في `entities/system-error/model/labels.ts` صارت مفاتيح (`*_LABEL_KEYS`) تُحل عبر `t()`/`useLabel`؛ الشبكة والحوار ونموذج الحالة/الملاحظات والحذف اليدوي عبر `headerKey`/`t()`/`<Label>`/Zod. عناوين/رسائل/آثار الأخطاء الملتقطة تبقى بيانات.
- **إصلاحات بوابات:** `SystemErrorsTable` — ضبط اعتماد `useMemo` للأعمدة (`setSelectedId`) وتصنيف `Object.entries` في `resolveKeys` ليجتاز React Compiler و`tsc`.
- **الوثائق:** READMEs (`labels`، `shared/config`، `entities/document-schema`، `features/document-schema-manage`، `features/document-field-values`، `widgets/document-schema-editor`، `widgets/document-fields-panel`، `widgets/system-errors-table`، `entities/numbering-registry`، `features/numbering-manage`، `widgets/numbering-registry-table`، `pages/numbering`، `entities/document`، `features/audit-manage`، `widgets/audit-log-table`، `pages/audit`، `features/lookup-manage`، `widgets/lookup-admin`، `pages/lookups`، `features/system-error-manage`، `entities/system-error`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات المنطقة عدا التعليقات التوثيقية وفاصلة العطف العربية «، ».
- **متبقٍ ضمن المرحلة 4:** جولة المتصفح (دخول يدوي من المستخدم) لكل المناطق فقط. (تفاصيل الوثيقة #17 ✅ والمرفقات #18 ✅ والعلاقات #19 ✅ وب5 #15 ✅ والوحدات المتفرقة #20 ✅ منفَّذة أدناه.)
- **التالي:** المرحلة 5 (#16).

#### ب4-متبقٍ — تفاصيل الوثيقة + حوارات document-manage/access-grant (#17) ✅ منفَّذة (2026-11-18)

تغطي صفحة `/documents/:documentId` كاملة: التبويبات الستة، الشريط الجانبي للمعلومات، أقسام الأرقام/التصاريح/الحقول/السجلات، الخط الزمني الموحّد، تبويبا الحركات والمشاركين/الأشخاص، وحوارات التسجيل/الإلغاء/إضافة مشارك/منح وصول/إلغاء تصريح.

- **الكتالوج:** أُضيفت `document-details.ts` (`DOCUMENT_DETAILS_LABELS`, `docdetails.*` ~180 مفتاحًا) ودُمجت في `LABEL_DEFAULTS`؛ ووُسّع `documents.ts` بمفاتيح وصف الصفحة المركّبة (`documents.page-description*`). إعادة استخدام `documents.create.*`/`documents.number-status.*`/`common.action.*` حيث تنطبق.
- **الكيانات:** `DocumentStatusBadge` (خمس حالات + سقوط `#id` عبر `useLabel`)، `OfficialNumberTag` («بدون رقم» عبر `useLabel`). الأرقام/العناوين تبقى بيانات.
- **الحوارات + Zod:** `features/document-manage` (register/participant/cancel) و`features/document-access-grant` (grant/revoke) عبر `t()`/`<Label>`/`FormField labelKey`؛ بطاقات نوع المشارك عبر `<Label>`؛ خيارات وجهة التسجيل عبر `resolveLabel` + اشتراك overrides؛ رسائل Zod عبر `t()` عند تحميل الوحدة. أسماء المستخدمين/الأقسام/الجهات/الأدوار والرموز والأمثلة اللاتينية تبقى بيانات.
- **الصفحة:** `DocumentDetailsPage` (تبويبات `labelKey` + `as const satisfies`، أزرار الرأس، شارة «قريبًا»، أسباب التجميد)، `DocumentDetailsTab` (صفوف المعلومات `labelKey`، عناوين/تلميحات الأقسام عبر `t()` لأن `CollapsibleSection` title/hint نصوص)، `DocumentNumbersSection` (إعادة كتابة كاملة: خرائط الحالة/الحدث `LabelKey`، أعمدة `headerKey`، حل النطاق/الحالة عبر `resolveLabel` + overrides، قوائم الصف/toasts النسخ عبر `t()`)، `DocumentMovementsTab` (تسميات نهاية الخطوة عبر `t()`، بطاقات/سلسلة الخطوات/سجل الأحداث عبر `<Label>`)، `DocumentTimeline` (أحداث الأرقام/الأرشفة/الحالة/المشاركين عبر `<Label>`/`resolveLabel`)، `DocumentGrantsSection` (شارات الحالة `LabelKey` + `useLabel`، العدّاد عبر `t()`)، `DocumentPersonsTab`/`DocumentParticipantsTab`/`DocumentFilesTab`/`DocumentsPage` (وصف الصفحة المركّب عبر `t()`). `ParticipantTarget` بيانات فقط (بلا ترحيل).
- **الوثائق:** READMEs (`entities/document`، `features/document-manage`، `features/document-access-grant`، `pages/documents`، `labels`، `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات الوحدة عدا التعليقات التوثيقية وصفوف Change Log في READMEs.
- **التالي:** المرفقات (#18)، ثم العلاقات (#19)، ثم ب5 (#15/#20)، ثم المرحلة 5 (#16).

#### ب4-متبقٍ — المرفقات (attachments panel + upload + document-file) (#18) ✅ منفَّذة (2026-11-18)

تغطي منطقة المرفقات (module 06): شبكة ملفات الوثيقة، الحوارات الخمسة (رفع ملف/رفع إصدار/أرشفة/تحقق من البصمة/الإصدارات)، ملخص الملف المختار، مساعد البصمة المحلية، وشارتا حالة الملف/البصمة.

- **الكتالوج:** أُضيفت `attachments.ts` (`ATTACHMENTS_LABELS`, `attachment.*`) ودُمجت في `LABEL_DEFAULTS`. إعادة استخدام `common.action.*`/`documents.create.*`/`documents.toast.*` حيث تنطبق.
- **الكيانات:** `FileArchivedBadge` (`attachment.state.*`) و`ChecksumBadge` (`attachment.checksum.*`) عبر خرائط `LabelKey` + `useLabel`.
- **الشبكة:** رؤوس الأعمدة عبر `headerKey`؛ خيارات مرشح الحالة/النوع عبر `resolveLabel` + اشتراك overrides؛ قوائم الصف/الرأس وtoasts النسخ/التنزيل عبر `t()`؛ caption/البحث/aria/الفراغ عبر `t()`؛ أزرار الرفع/المسح وشارة المسح الأساسي عبر `<Label>`. عناوين/أسماء الملفات و`descAr` للنوع تبقى بيانات.
- **الحوارات + المساعد + Zod:** العناوين/الأوصاف/الـ toasts/الـ placeholders/aria/نصوص التقدم عبر `t()`؛ النصوص الظاهرة والشارات والأزرار عبر `<Label>`؛ تسميات الحقول عبر `FormField labelKey`؛ خانة المسح الأساسي عبر `t()`؛ حدود الحجم/الامتدادات عبر `<Label params>` مع إبقاء قائمة الامتدادات `dir="ltr"` وفاصلة العطف «، ». رسائل Zod تُبقي لاحقة كود `Attachments.*` عبر `withCode(t(...))`. أسماء الملفات/الأصول/البصمات/الرافع تبقى بيانات.
- **الوثائق:** READMEs (`widgets/document-attachments-panel`، `features/attachment-upload`، `entities/document-file`، `labels`، `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ عدا التعليقات التوثيقية وفاصلة العطف «، » في ضم الامتدادات.
- **التالي:** العلاقات (#19)، ثم ب5 (#15/#20)، ثم المرحلة 5 (#16).

#### ب4-متبقٍ — العلاقات (relations panel + manage + entity) (#19) ✅ منفَّذة (2026-11-18)

تغطي منطقة العلاقات (module 08): شبكة روابط الوثيقة، مبدّل العرض (جدول/مخطط/شجرة) وإشعار الروابط المحجوبة، مخطط Mermaid/الشجرة مع خيار العمق، سجل الروابط الدائم، وحوارا إضافة/إزالة رابط مع منتقي الوثيقة.

- **الكتالوج:** أُضيفت `relations.ts` (`RELATIONS_LABELS`, `relation.*`) ودُمجت في `LABEL_DEFAULTS`. إعادة استخدام `common.action.retry`/`documents.toast.*` حيث تنطبق.
- **الكيان:** `direction.ts` صار خرائط `LabelKey` (`PHRASE_KEYS`/`FALLBACK_KEYS`) مع `relationPhraseKey(id, direction)` و`relationPhrase` يُحل عبر `t()` للمستهلكين غير التفاعليين (الفرز/الطباعة)؛ `RELATION_DIRECTION_LABELS` خريطة `LabelKey`. `RelationDirectionLabel` يُحل تفاعليًا عبر `useLabel`. أُصدّر `relationPhraseKey` من الـ barrel.
- **الشبكة:** رؤوس الأعمدة عبر `headerKey`؛ خيارات مرشح النوع/الاتجاه عبر `resolveLabel` + اشتراك overrides؛ قوائم الصف/الرأس وtoasts النسخ عبر `t()`؛ caption/البحث/aria/الفراغ عبر `t()`؛ زرا الإضافة/المسح عبر `<Label>`. `relationPhrase` يبقى في `sortValue`. عناوين/أرقام الوثائق و`descAr` للنوع تبقى بيانات.
- **اللوحة/المخطط/السجل:** مبدّل العرض (`labelKey` + `<Label>`)، إشعار الروابط المحجوبة (`t()` مع `{count}`)، عنوان السجل عبر `<Label>`؛ خيارات العمق عبر `resolveLabel` + overrides، إشعار الاقتطاع/الفراغ/«مؤرشفة» وaria عبر `t()`/`<Label>`؛ سجل الأحداث (الفراغ/المصدر←الهدف/المنفّذ/السبب/السابق/التالي) عبر `<Label>`. `node.title`/`event.reason`/أسماء المنفّذين تبقى بيانات.
- **الحوارات + المنتقي + Zod:** العناوين/الأوصاف/الـ toasts/الـ placeholders/aria عبر `t()`؛ الأزرار الظاهرة عبر `<Label>`؛ تسميات الحقول عبر `FormField labelKey`؛ منتقي الوثيقة (تغيير/بحث/نتائج/فراغ) عبر `t()`/`<Label>`. رسائل Zod عبر `t()` عند تحميل الوحدة. عناوين الوثائق وأسماء الأنواع تبقى بيانات.
- **الطباعة:** `DocumentPrintSheet` خلية اتجاه الرابط تُحل عبر `t(RELATION_DIRECTION_LABELS[direction])` (بقية نص القالب يُرحّل في ب5).
- **الوثائق:** READMEs (`entities/document-relation`، `features/document-relation-manage`، `widgets/document-relations-panel`، `widgets/document-print-sheet`، `labels`، `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات الوحدة عدا التعليقات التوثيقية وصفوف READMEs.
- **التالي:** ب5 (#15/#20)، ثم المرحلة 5 (#16).

#### ب5 — dashboard/notifications/auth/print + متبقي التدقيق (#15) ✅ منفَّذة (2026-11-18)

تغطي شاشة الدخول وتغيير كلمة المرور وحالة المنع، لوحة التحكم، منطقة الإشعارات (الجرس + الصندوق + اتصال SignalR)، ورقة طباعة الوثيقة كاملة، وتبويبات صفحة التدقيق.

- **الكتالوج:** أُضيفت `auth.ts` (`AUTH_LABELS`, `auth.*`)، `dashboard.ts` (`DASHBOARD_LABELS`, `dashboard.*`)، `notifications.ts` (`NOTIFICATIONS_LABELS`, `notification.*`)، `print.ts` (`PRINT_LABELS`, `print.*`)، ووُسّعت `audit.ts` بمفاتيح التبويبات (`audit.tab.*`/`audit.page.*`)؛ دُمجت كلها في `LABEL_DEFAULTS`. إعادة استخدام `common.action.retry`/`table.refresh`/`documents.toast.*` حيث تنطبق.
- **المصادقة:** نماذج الدخول/تغيير كلمة المرور ورسائل Zod عبر `t()`؛ تسميات الحقول عبر `FormField labelKey`؛ الأزرار/العناوين/البطاقات الظاهرة عبر `<Label>`؛ حالة المنع وزر الخروج عبر `<Label>`. `PasswordField`/`Select`/aria/placeholders عبر `t()`.
- **لوحة التحكم:** مصفوفة الإحصاءات تحمل `labelKey`؛ الترحيب/البطاقات/قسم «ما التالي؟» عبر `<Label>`؛ اسم المؤسسة يبقى `<BrandName>` بيانات.
- **الإشعارات:** الجرس (الشارة/حالة الاتصال/العنوان/قراءة الكل/الفراغ/عرض الكل/aria الصفوف) عبر `<Label>`/`t()`؛ الصندوق (رؤوس الأعمدة `headerKey`، المرشحات عبر `resolveLabel` + overrides، قوائم الصف/الرأس، caption/البحث/النسخ/الفراغ) عبر `<Label>`/`t()`؛ toast الـ hub («فتح») وaria غير المقروء عبر `t()`. عناوين/نصوص الإشعارات تبقى بيانات الخادم.
- **الطباعة:** كل القالب — عناوين الأقسام، تسميات الحقائق، رؤوس الجداول، markers (مؤرشف/نعم/لا)، وإشعار الروابط المحجوبة — يُحل عبر `t()` من `print.*`. أسماء القوائم/الأرقام الرسمية/تسميات الحقول/الأسباب تبقى بيانات.
- **التدقيق:** تبويبات الصفحة (`PANELS`) تحمل `labelKey`/`hintKey` عبر `<Label>`، وaria قائمة التبويبات عبر `t('audit.page.tabs-aria')`.
- **الوثائق:** READMEs (`features/auth`، `pages/login`، `pages/change-password`، `pages/dashboard`، `entities/notification`، `features/notification-live`، `widgets/notification-bell`، `widgets/notification-inbox`، `widgets/document-print-sheet`، `pages/audit`، `labels`، `shared/config`) + هذه الخطة، بنفس الدور. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات ب5 عدا التعليقات التوثيقية وفاصلة الجمع العربية «، ».
- **التالي:** الوحدات المتفرقة (#20)، ثم المرحلة 5 (#16).

#### ب5-متبقٍ — مشتركات/كيانات متفرقة (ui badges, shell, guides, misc) (#20) ✅ منفَّذة (2026-11-18)

تُكمل ما تبقى من نصوص الواجهة الثابتة خارج المناطق: بدائيات `shared/ui` المتبقية، أدوات `shared/lib`، رسائل `shared/api`، بوابة الإقلاع، تنقّل السجلات، شارات الكيانات ومنتقي الموظف وخريطة الوحدات، عنوان التبويب، ومحرّر التسميات نفسه.

- **الكتالوج:** وُسّعت `common.ts` (select/password/theme/label-edit/tab-title/boot/draft/size/checksum/api/mermaid/error-boundary + `shell.register-nav-aria`)؛ أُضيفت `guides.ts` (`GUIDE_LABELS` — كروم الشرح فقط، لا محتوى الدروس) و`label-editor.ts` (`LABEL_EDITOR_LABELS`)؛ ووُسّعت `access.ts` (`user.scope.*`، `role.badge.*`/`role.module.*`، `employee.picker.*`)؛ دُمجت كلها في `LABEL_DEFAULTS`. إعادة استخدام `common.action.save`/`cancel`/`close`/`retry`.
- **shared/ui:** `theme-toggle`/`select`/`password-field`/`dialog`/`label` عبر `t()`؛ `mermaid-view`/`guide`/`error-boundary` عبر `<Label>`/`t()`؛ `guide-tour` (driver.js next/prev/done/progress) عبر `t()` مع إبقاء `{{current}}`/`{{total}}` حرفية.
- **shared/lib + api + app:** `format.ts`/`crypto.ts`/`formDraft.ts` تستورد `t` نسبيًا من `./labels` (لا دورة عبر الـ barrel)؛ `problemDetails.ts` عبر `t()` (آمن: `shared/lib` لا يستورد `shared/api`)؛ `LabelsBootGate` عبر `t()`.
- **الكيانات:** `UserScopeBadge` خريطة `labelKey` + `useLabel` غير مشروط (مع fallback `نطاق #{id}`)؛ `EmployeePicker` عبر `t()`/`<Label>`؛ `RoleBadge` aria عبر `t()`؛ `moduleLabels.ts` خريطة `LabelKey` + `t()` (المستهلك الوحيد هو الدالة)؛ `BrandingEffects` عنوان التبويب عبر `t('common.tab-title', { name })` والاسم يبقى بيانات.
- **المحرّر:** `LabelEditor` (toastات + كل aria/title) و`schemas.ts` (رسائل Zod الأربع) عبر `t()` من `label-editor.*`.
- **الاستثناءات:** التعليقات التوثيقية، فاصلة العطف «، », محتوى الدروس في `shared/config/guides/*`، `DEFAULT_BRAND_NAME`، وأسماء الكيانات/البيانات. لا تغيير endpoints/صلاحيات (واجهة فقط).
- **الوثائق:** READMEs (`shared/ui`، `shared/lib`، `shared/api`، `app/providers`، `widgets/app-shell`، `entities/user`، `entities/employee`، `entities/role`، `entities/branding`، `features/ui-label-manage`، `labels`، `shared/config`) + هذه الخطة، بنفس الدور.
- **البوابات:** `npm run lint` ✅ (115/20/118) + `npm run build` ✅ (تحذير حجم الحزم الموجود مسبقًا فقط). فحص grep `[؀-ۿ]`: لا نص عربي حرفي متبقٍ في ملفات الوحدة عدا التعليقات التوثيقية وفاصلة العطف «، ».
- **التالي:** المرحلة 5 (#16) — حارس الاتساق + الإغلاق.

### المرحلة 5 — حارس الاتساق + الإغلاق ✅ منفَّذة (2026-11-18)

1. ✅ سكربت الفحص `scripts/check-label-keys.mjs` (node) موصول بـ `npm run lint` (+ `npm run check:labels`): يقرأ كل `labels/*.ts` (مفتاح/افتراضي)، ويفشل عند تكرار مفتاح، أو مفتاح غير معروف في استدعاءات التسمية الصريحة (`t`/`useLabel`/`resolveLabel`، `<Label k>`، `labelKey`/`headerKey`/`hintKey`)، أو placeholder مشوّه — مع قائمة بيضاء للرموز الحرفية `{YEAR}`/`{SEQ}`/`{SEQ:D6}` وdriver.js `{{current}}`/`{{total}}` — ويحذّر للمفاتيح المهجورة (غير المستخدمة في المصدر). وجود المفاتيح المكتوبة يفرضه أيضًا `tsc -b`. النتيجة: ~3006 مفتاح، 0 تكرار/0 مجهول/0 مشوّه/0 مهجور؛ أُزيل `common.action.delete` غير المستخدم.
2. ⏭️ (اختياري) قاعدة lint ضد السلاسل العربية الحرفية الجديدة: **مُرجأة** — السكربت + فحص grep `[؀-ۿ]` لكل دفعة يغطيان الغرض دون إيجابيات كاذبة على النص البياناتي.
3. ✅ التوثيق النهائي: `knowledge.md` (فقرة اكتمال المرحلة 4 + فقرة المرحلة 5 + سطر بوابة lint)، `README.md` الجذر (صف `scripts/` + سطر Change Log موحّد للمراحل 1–5)، وREADMEs كل مجلد تغيّر. الدليل القصير داخل الوضع = الشريط اللاصق الموجود («وضع تعديل التسميات مفعّل — انقر أي نص لتعديله…») وهو بيانات كروم مرحّلة. لا تغيير endpoints/صلاحيات في المرحلة 5.
4. ✅ البوابات الأمامية: `npm run lint` (eslint + guide 115/20/118 + label 3006/0) + `npm run build` (tsc + vite؛ تحذير حجم الحزم الموجود مسبقًا فقط). بوابتا الخلفية (`dotnet build`/`test`) أُنجِزتا في المرحلة 0 ولم تتغير الخلفية منذ ذلك (المراحل 1–5 واجهة فقط). ⏳ **متبقٍ:** جولة المتصفح الشاملة بثلاثة أدوار (أدمن/موظف/بلا صلاحية) فاتح/داكن — تتطلب دخولًا يدويًا من المستخدم (لا أسرار في السكربتات).

---

## 4. البوابات لكل مرحلة (Definition of Done)

- [ ] `dotnet build` + `dotnet test` (مراحل الخلفية) — مع إيقاف API 5028 أولاً (نسأل إن كان جلسة VS).
- [ ] `npm run lint` + `npm run build` خضراء.
- [ ] README لكل مجلد تغيّر + سطر Change Log، بنفس الدور.
- [ ] `knowledge.md` محدّث عند تغيّر endpoints/صلاحيات.
- [ ] لا أسرار في الكود، لا commits إلا بطلب المستخدم.
- [ ] تحقق متصفح فعلي (دخول يدوي من المستخدم): RTL + LTR حيث يلزم، فاتح + داكن، الأدوار الثلاثة عند وجود صلاحيات.
- [ ] توقف ومراجعة المستخدم قبل المرحلة التالية.

## 5. المخاطر والتخفيف

| الخطر | التخفيف |
| --- | --- |
| ضخامة الترحيل (~3188 سلسلة) | دفعات مستقلة قابلة للشحن؛ التطبيق يعمل مختلطاً (نص مرحّل ونص حرفي) بلا كسر أثناء الانتقال |
| نسيان نص خارج القاموس | سكربت الحارس (مرحلة 5) + فحص grep لكل دفعة |
| كسر placeholders عند التعديل | تحقق клиент قبل الحفظ + رفض برسالة واضحة |
| وميض/تأخر الإقلاع | cache محلي + stale-while-revalidate؛ الشاشة فقط عند أول مرة أو فشل |
| تسرّب وضع التعديل لغير الأدمن | إخفاء المفتاح من DOM + سياسة على السيرفر (دفاع مزدوج) |
| تعديل مصطلح مشترك يغيّر أماكن غير مقصودة | مقصود وموثّق؛ الشريط العلوي يذكّي أن التعديل عام، والاستعادة للافتراضي متاحة دائماً |
| نص طويل يكسر التخطيط | حد 200 حرف + اختبار بصري لأطول التسميات في الجداول والـ sidebar |

## 6. امتدادات مستقبلية (خارج النطاق، لا تُبنى الآن)

- صفحة مركزية «إدارة التسميات» (جدول بكل المفاتيح + بحث + استعادة جماعية) — تخدم النصوص غير المرئية دائماً (toasts، رسائل الخطأ).
- قاموس مشترك مع السيرفر لتنعكس التسميات على الإشعارات والتدقيق والطباعة.
- تصدير/استيراد حزمة تسميات (JSON) لنقل مصطلحات مؤسسة إلى أخرى.
- تعدد لغات.