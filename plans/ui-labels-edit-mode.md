# الخطة — قاموس التسميات القابل للتعديل + وضع التعديل المباشر (UI Labels)

**الحالة:** معتمدة — **المراحل 0 و1 و2 و3 منفَّذة (2026-11-17)**؛ المرحلتان 4–5 بانتظار التنفيذ.
**النطاق:** واجهة SPA (`institutionaldiwan.front`) + طرف API جديد صغير في `InstitutionalDiwan.Server` (جدول واحد + 3 endpoints). لا تعديل على نصوص السيرفر المولّدة (إشعارات/تدقيق/أخطاء تحقق) — خارج النطاق عمداً، والتصميم لا يمنع إضافتها لاحقاً.
**آخر مراجعة:** 2026-11-17.
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

### المرحلة 5 — حارس الاتساق + الإغلاق

1. سكربت فحص (node) يُشغَّل مع البوابات: كل مفتاح مستخدم في JSX/`t()` موجود في الكتالوج، وكل مفتاح في الكتالوج مستخدم (تحذير للمهجور)، ولا placeholders مفقودة.
2. (اختياري، يُقرَّر عند المراجعة) قاعدة lint تمنع سلاسل عربية حرفية جديدة في JSX خارج الكتالوج.
3. توثيق نهائي: `knowledge.md` (الـ endpoints الجديدة)، READMEs، ودليل قصير داخل الوضع نفسه.
4. البوابات الكاملة الأربع + جولة متصفح شاملة بثلاثة أدوار (أدمن/موظف/بلا صلاحية) فاتح/داكن.

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