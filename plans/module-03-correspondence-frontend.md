# خطة الموديول 3 — المراسلات (واجهة SPA)

> **الحالة:** معتمدة — نُفّذت Phase 0 وPhase 1 وPhase 2 (2026-10-28)؛ Phase 3 وما بعدها
> لم تبدأ وتنتظر الموافقة.
> **النطاق:** واجهة `institutionaldiwan.front` للموديول 3، مع تغيير خلفي صغير واحد
> (Phase 0) لفتح قراءات الاختيار التي تحتاجها الشاشات.
> **الخلفية:** خلفية الموديول 3 مكتملة ومختبرة (480 اختبارًا ناجحًا). مجلدات الشرائح
> الأمامية موجودة كتوثيق فقط، ولا يوجد أي كود أو مسار أو ترجمة أخطاء بعد.

---

## 1. الوضع الحالي المؤكد (تم فحصه)

| العنصر | الحالة |
| --- | --- |
| خلفية الوثائق/الأرقام/الحركات/الجهات الخارجية/التصاريح | منفذة ومختبرة |
| `src/entities/{document,document-file,movement,numbering-registry}` | README فقط |
| `src/features/{document-create,document-access-grant,attachment-upload,movement-routing}` | README فقط |
| `src/widgets/{document-registry-table,document-attachments-panel,movement-inbox}` | README فقط |
| `src/pages/{documents,movements,numbering}` | README فقط |
| المسارات في [routes.ts](file:///C:/Users/ramia/source/repos/InstitutionalDiwan/institutionaldiwan.front/src/shared/config/routes.ts) | `DOCUMENTS`/`DOCUMENT_DETAIL`/`MOVEMENTS`/`NUMBERING` معرّفة بلا شاشات |
| عناصر القائمة في [shell-nav.ts](file:///C:/Users/ramia/source/repos/InstitutionalDiwan/institutionaldiwan.front/src/widgets/app-shell/model/shell-nav.ts) | الوثائق/الحركات/الترقيم `available: false` |
| `errorMessages.ts` | لا يحتوي أي رمز من `Correspondence.*` / `Numbering.*` / `Movements.*` / `Attachments.*` |
| التصاريح | 16 رمزًا؛ المطلوبة هنا: `DOCUMENT_VIEW/CREATE/EDIT`، `NUMBERING_VIEW/MANAGE`، `ACCESS_GRANT_MANAGE` |

## 2. قرارات معمارية للخطة

1. **FSD كما هو معتمد:** `pages → widgets → features → entities → shared`، وكل شريحة
   تُصدّر واجهتها العامة من `index.ts`، و`README.md` لكل مجلد يتحدث مع الكود في نفس المرحلة.
2. **طلبات الخادم هي المصدر:** كل القوائم الكبيرة (`documents`, `users`, `employees`) مُصفّحة
   على الخادم؛ القوائم المحدودة (تصاريح الوثيقة، ملفاتها، حركاتها، سجلات الترقيم) تُعاد كمصفوفة
   وتُفلتر محليًا فقط عند الحاجة.
3. **حالة الجدول في الرابط:** نستخدم `useTableState` نفسه المعتمد في شاشتي المستخدمين والموظفين،
   مع `sortBy`/`sortDescending` للوثائق (`createdAt` افتراضيًا).
4. **الصلاحيات تُخفي ولا تُعطّل فقط عند البناء:** العناصر التي يفتقدها المستخدم تُخفى من
   القوائم والأزرار، ويبقى الخادم هو الحكم النهائي (403).
5. **لا شاشة جديدة للجهات الخارجية:** تُدار من شريحة مستقلة لكن تُعرض داخل صفحة الوثائق كتبويب
   إداري، لأن رموز الصلاحية نفسها (`DOCUMENT_VIEW`/`DOCUMENT_EDIT`).
6. **المرفقات (الموديول 06) ليست نطاق هذه الخطة:** تبقى تبويبًا معلَّمًا «قريبًا» في تفاصيل
   الوثيقة، وتُنفذ في خطة الموديول 06.

## 3. الثغرة الخلفية الوحيدة المكتشفة (Phase 0)

شاشات الموديول 3 تحتاج قوائم اختيار لا يستطيع حامل صلاحية الوثائق قراءتها اليوم:

| الحاجة | الموجود | الأثر |
| --- | --- | --- |
| اختيار قسم داخل الوثيقة (المشاركون/الحركات) | `GET /api/departments` يتطلب `DEPARTMENT_MANAGE`؛ `identity-options/departments` يتطلب `USER_MANAGE` أو `EMPLOYEE_MANAGE` أو `DEPARTMENT_MANAGE` | حامل `DOCUMENT_CREATE/EDIT` فقط لا يستطيع اختيار قسم |
| اختيار مستخدم (إسناد حركة، تصريح وصول) | لا يوجد خيار مستخدمين؛ `GET /api/users` يتطلب `USER_MANAGE` | لا يمكن بناء نموذج التصريح أو الإسناد |
| عرض أسماء المشاركين/المكلَّفين/أصحاب التصاريح | الاستجابات تُرجع معرفات فقط | الواجهة ستعرض GUID |

**التغيير المقترح (خلفي، محدود ومختبر):**
- توسيع `GET /api/identity-options/departments` ليقبل أيضًا `DOCUMENT_CREATE` أو `DOCUMENT_EDIT`
  في `CanReadAsync` (نفس المشروع الحالي، بلا بيانات جديدة حسّاسة).
- إضافة `GET /api/identity-options/users?search=&page=&pageSize=` يُرجع
  `{ userId, username, isActive }` فقط، ويُحمى بأي من `USER_MANAGE` أو `ACCESS_GRANT_MANAGE`
  أو `DOCUMENT_EDIT`، مع بحث وترقيم ونفس نمط الـ handler الحالي.
- اختبارات: توسيع `IdentityOptions` في `InstitutionalDiwan.Tests`، وتوثيق
  `IdentityOptionsController` و`Application/Features/README.md` و`knowledge.md`.

## 4. المراحل

كل مرحلة تنتهي بتشغيل البوابات والتوثيق، ثم **توقف للمراجعة** قبل المرحلة التالية.

### Phase 0 — تمكين الاختيارات (خلفي) — ✅ منفذة
- **المخرجات:** endpoint المستخدمين الجديد + توسيع الأقسام + اختبارات + توثيق.
- **قبول:** `dotnet build` و`dotnet test` ينجحان؛ الطلبات تُرجع 403 لمن لا يملك أي صلاحية
  مسموحة؛ لا اتصال بيانات أو أدوار في استجابة خيارات المستخدمين.

### Phase 1 — الأساس المشترك للمراسلات — ✅ منفذة
- **الشرائح:** `entities/document` (types, query keys, `useDocuments`, `useDocumentById`,
  `useDocumentDetails`, `useDocumentTimeline`, `useDocumentAccessGrants`، شارات الحالة/الاتجاه/
  الرقم الرسمي).
- **الخطأ العربي:** إضافة كامل رموز `Correspondence.*` و`Numbering.*` و`Movements.*` و
  `Attachments.*` (المستخدمة في هذه الخطة) إلى `shared/api/errorMessages.ts` بصياغة عربية.
- **التنقل:** لا تغيير في Phase 1 — يُفعَّل كل عنصر تنقل في المرحلة التي تسلّم صفحته
  (الوثائق في Phase 2، الترقيم في Phase 5، الحركات في Phase 6) حتى لا يشير رابط إلى صفحة غير موجودة.
- **المسارات:** تسجيل `/documents` و`/documents/:documentId` مع صفحتهما في Phase 2.
- **قبول:** الشارات تعرض التسميات العربية من lookups؛ حالة تحميل/فراغ/خطأ موحدة؛ `build`+`lint`.

### Phase 2 — سجل الوثائق والإنشاء — ✅ منفذة (2026-10-28)
- **الشرائح:** `widgets/document-registry-table` (بحث، ترقيم صفحات، فرز، مرشحات الحالة/
  الاتجاه/النوع/القسم المالك، عمود الرقم الرسمي `dir="ltr"`، قائمة صف وصلاحيات).
  `features/document-create` (نموذجان: إنشاء فوري برقم رسمي يدوي + `NumberingYear`، وحفظ كمسودة
  عبر `POST /api/documents/drafts`)، مع `identity-options` للأقسام والجهات الخارجية.
- **الصفحة:** `pages/documents/DocumentsPage` + `index.ts`، مع تسجيل `/documents` في الراوتر
  وتفعيل عنصر «الوثائق» في القائمة.
- **نقاط النهاية:** `GET /api/documents`، `POST /api/documents`، `POST /api/documents/drafts`،
  `GET /api/external-organizations` (للمشاركين)، `GET /api/identity-options/departments`.
- **قبول:** إنشاء وثيقة مسجلة ومسودة من المتصفح، ظهورهما في السجل فورًا، أخطاء
  `Numbering.OfficialNumberTaken` و`Correspondence.TitleEmpty` بالعربية، وتحديث ذاكرة الاستعلام.
- **انحرافات موثقة عند التنفيذ:**
  1. أُضيف `DOCUMENT_VIEW` إلى صلاحيات `identity-options/departments` (مع تحديث مصفوفة الأمان)
     لأن قارئ السجل فقط يحتاج أسماء الأقسام المالكة ومرشح القسم، وهو ما لم تلتقطه Phase 0.
  2. لم يُسجّل `/documents/:documentId` بعد: لا توجد صفحة تفاصيل قبل Phase 3، وتسجيل مسار
     بلا صفحة يخالف مبدأ «لا رابط لصفحة غير موجودة» في Phase 1؛ يُسجّل مع `DocumentDetailsPage`.
  3. لم يُستخدم `GET /api/external-organizations` في الإنشاء: الخادم يعيّن القسم المالك من قسم
     المستخدم الأساسي، والجهات الخارجية تخصّ المشاركين في Phase 3.
  4. الوضع الافتراضي في النموذج مسودة، ويصبح «مسجّل» افتراضيًا فقط لحامل `NUMBERING_MANAGE`.

### Phase 3 — تفاصيل الوثيقة ودورة حياتها
- **الشرائح:** `features/document-manage` (تعديل مسودة، تسجيل مسودة برقم من السجل، إضافة مشارك،
  إلغاء الوثيقة بسبب)، `entities/document` يكتمل بـ details/timeline.
- **الصفحة:** `DocumentDetailsPage` بتبويبات `?tab=details|participants|numbers|movements|files|grants`؛
  المشاركون يعرضون أسماء الأقسام/الجهات/الأشخاص، والخط الزمني يعرض أحداث الحالة/المشاركين/الرقم.
- **نقاط النهاية:** `GET .../details`، `GET .../timeline`، `PUT /api/documents/{id}`،
  `POST .../register`، `POST .../participants`، `POST .../cancel`.
- **قبول:** كل انتقال حالة متاح يظهر فقط عندما تسمح به الحالة والصلاحية؛ رسالة
  `Correspondence.OnlyDraftEditable` / `Correspondence.HasActiveMovements` مفهومة عربيًا.

### Phase 4 — تصاريح الوصول (بقايا الموديول 2)
- **الشرائح:** `features/document-access-grant` (إضافة تصريح: مستخدم + مستوى + سبب + انتهاء اختياري،
  وإلغاء تصريح بسبب)، واستخدام `GET /api/identity-options/users` من Phase 0.
- **الصفحة:** تبويب `grants` داخل تفاصيل الوثيقة.
- **نقاط النهاية:** `GET/POST/DELETE /api/documents/{documentId}/access-grants`
  (`ACCESS_GRANT_MANAGE`).
- **قبول:** من يملك `ACCESS_GRANT_MANAGE` فقط يستطيع فتح النموذج واختيار مستخدم؛ لا يظهر
  التبويب لغيره؛ التصاريح الملغاة تظهر كسجل مع تاريخ الإلغاء.

### Phase 5 — الترقيم
- **الشرائح:** `entities/numbering-registry` (سجلات + عدادات)، `pages/numbering` لإدارة السجلات
  (CRUD + تفعيل/إيقاف، `NUMBERING_VIEW` قراءة و`NUMBERING_MANAGE` كتابة)، وقسم الأرقام داخل
  تفاصيل الوثيقة: إصدار رقم، إلغاء، استبدال، سجل الأرقام، وعرض `registry-entries`.
- **تسجيل القسم:** نموذج `department-registration` (رقم مكتوب + تاريخ) يظهر داخل قسم الأرقام
  عندما يتطلب مسار الحركة تسجيل القسم الوجهة لرقمه.
- **نقاط النهاية:** كل مسارات `DocumentNumbersController` و`NumberingRegistriesController`
  وسجل القسم `GET /api/departments/{id}/register`، مع تسجيل `/numbering` وتفعيل عنصر «الترقيم».
- **قبول:** إصدار رقم على وثيقة مسجلة، تعارض `Numbering.OfficialNumberTaken` عربيًا، ومنع
  العمليات لمن لا يملك `NUMBERING_MANAGE`.

### Phase 6 — الحركات
- **الشرائح:** `entities/movement` (أنواع/حالة/خطوات/أحداث + مفاتيح استعلام)،
  `features/movement-routing` (فتح حركة، إرسال، استلام، إكمال، رفض، إرجاع، إعادة توجيه، إغلاق،
  إلغاء)، `widgets/movement-inbox` (صندوق وارد/صادر القسم)، `pages/movements`.
- **الصفحة:** `/movements` بصندوقين وارد/صادر، وتبويب `movements` في تفاصيل الوثيقة يعرض الخط
  الزمني للحركة وأزرار الإجراءات المسموحة.
- **قواعد يجب أن تعكسها الواجهة:** الديوان المركزي إلزامي لفتح الحركة
  (`Movements.CentralDiwanRoutingRequired`)، لا يمكن الاستلام قبل تسجيل رقم القسم الوجهة
  (`Numbering.DepartmentRegistrationRequired`)، والرفض يعيد التوجيه إلى الديوان المركزي.
- **نقاط النهاية:** `POST /api/movements` والإجراءات التسعة، `GET /api/movements/inbox|outbox`
  و`by-document/{id}`، مع `identity-options` للأقسام/المستخدمين، وتسجيل `/movements` وتفعيل
  عنصر «الحركات».
- **قبول:** تنفيذ دورة كاملة من المتصفح بين قسمين + رفض + إغلاق، مع تحديث الصندوق والخط الزمني.

### Phase 7 — الجهات الخارجية
- **الشرائح:** `entities/external-organization` (قراءات + خيارات)،
  `features/external-organization-manage` (إنشاء/تعديل/إيقاف/تفعيل).
- **الموضع:** تبويب إداري داخل صفحة الوثائق (لا عنصر تنقل جديد)، يظهر بقدر `DOCUMENT_VIEW`،
  وأزرار الكتابة بقدر `DOCUMENT_EDIT`.
- **نقاط النهاية:** `GET/POST/PUT/DELETE /api/external-organizations` و`POST .../activate`.
- **قبول:** تكرار الرمز يعرض `Correspondence.ExternalOrgCodeAlreadyExists` عربيًا، والإيقاف
  يمنع اختيار الجهة في المشاركين.

### Phase 8 — التحقق النهائي والتوثيق
- بوابات: `npm run build`, `npm run lint`, و`dotnet build`/`dotnet test` بعد تغييرات Phase 0.
- اختبار المتصفح: تسجيل دخول بأدوار مختلفة (مدير وثائق، مدير ترقيم، مدير تصاريح فقط)، وتجربة
  المسار الذهبي والحالات الحدّية (وثيقة مسودة، وثيقة ملغاة، حركة نشطة، رقم مستبدل).
- توثيق: README لكل مجلد متغير + `knowledge.md` + `README.md` الجذر + `pages/README.md`.

## 5. خارج النطاق صراحةً

- تنفيذ المرفقات (الموديول 06): يبقى تبويبًا معلَّمًا فقط.
- الأرشفة (07)، العلاقات (08)، الإشعارات، الإحصاءات.
- أي تغيير في عقود الخلفية غير المذكور في Phase 0.

## 6. المخاطر والقرارات

| الخطر/القرار | المعالجة المقترحة |
| --- | --- |
| توسيع `identity-options/departments` لصلاحيات الوثائق يوسّع سطح القراءة | أسماء أقسام فقط، بلا أرقام موظفين أو بيانات اتصال؛ نطاق ضيق ومختبر |
| إضافة خيار المستخدمين قد تكشف قائمة مستخدمين | المعرّف واسم المستخدم فقط، وترقيم وبحث، وبوابة صلاحيات صريحة |
| ازدواجية «إنشاء برقم رسمي» مقابل «مسودة ثم تسجيل» | نموذجان واضحان في نموذج الإنشاء، والافتراضي حفظ كمسودة إلا لمن يملك `NUMBERING_MANAGE` |
| حجم الشاشات | تقسيم تفاصيل الوثيقة إلى تبويبات مستقلة، وكل تبويب شريحة/مكوّن صغير |

## 7. تسلسل التنفيذ

```
Phase 0 → Phase 1 → Phase 2 → Phase 3 → Phase 4
                          └→ Phase 5 → Phase 6 → Phase 7 → Phase 8
```

Phase 4 يمكن أن تنفَّذ بالتوازي مع Phase 5 بعد اكتمال Phase 3، لكن الالتزام يبقى بمراجعة لكل مرحلة.
