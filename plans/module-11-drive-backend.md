# الخطة — المودول 11: نظام الملفات (Drive) — الباك إند

**الحالة:** معتمدة (2026-11-19). **المراحل 0 (Domain) و1 (Infrastructure) و2 (Application) و3 (Server) و4 (OnlyOffice) و5 (الاختبارات الشاملة) منفّذة بالكامل** وبواباتها خضراء (بناء 0 أخطاء، 1264 اختبارًا ناجحًا)، والتحقق الحي على المنفذ 5028 ناجح لمسارات البيانات الوصفية. **المودول مغلق في الباك إند** (2026-11-20). **مطلوب قبل التشغيل الحي الكامل:** عنوان SeaweedFS (Endpoint) ومفتاحا AccessKey/SecretKey عبر user-secrets (لمسارات بايتات الملف)؛ وعنوان خادم OnlyOffice وسر JWT (`Drive:OnlyOffice`) لجلسة تحرير فعلية. واجهة Drive بنمط Google Drive في خطة منفصلة لاحقة (`module-11-drive-frontend.md`)؛ بُذرت مفاتيح `drive.*` التأسيسية في كتالوج Ui labels تمهيدًا لها.
**النطاق:** `InstitutionalDiwan.Domain` / `Application` / `Infrastructure` / `Server` / `Tests`. واجهة Drive بنمط Google Drive في خطة منفصلة لاحقة (`module-11-drive-frontend.md`).
**آخر مراجعة:** 2026-11-20.

---

## 1. الهدف والقرارات المعتمدة من النقاش

وحدة ملفات مستقلة **لا علاقة لها بأرشفة الوثائق (المودول 07)**: مجلدات وملفات وصلاحيات ومعاينة وتحرير Word/Excel داخل الموقع وسجل إصدارات وسلة محذوفات وحصص، بتخزين SeaweedFS عبر واجهة S3.

| المحور | القرار المعتمد |
| --- | --- |
| المعمارية | وحدة جديدة داخل نفس الـ modular monolith: `Features/Drive` + `Entities/Drive`، بلا خدمات منفصلة |
| محرر Word/Excel | OnlyOffice Document Server ذاتي الاستضافة (النسخة المجتمعية، حد ~20 جلسة متزامنة — مقبول) |
| المساحات | «ملفاتي» شخصية لكل مستخدم + مساحات مشتركة ينشئها أي مستخدم (نمط Shared Drives) |
| الصلاحيات | وراثة هرمية من المجلد الأب، أدوار عرض/تحرير/إدارة، منح لمستخدمين أو أقسام، كسر وراثة اختياري، **بدون روابط مشاركة خارجية** |
| الحذف | طلب موافقة → رئيس القسم؛ الرئيس يحذف مباشرة حيث تسري صلاحيته؛ **يسري حتى على الملفات الشخصية**؛ الحذف النهائي من السلة حصري بـ ADMIN/رئيس القسم |
| الإصدارات | كل حفظ = إصدار غير قابل للتعديل، بلا حد عملي (حتى 1000+)، عرض/تنزيل/استرجاع |
| السلة | نعم، استرجاع + تنظيف تلقائي بعد 30 يومًا |
| البحث | بالاسم/النوع/التاريخ في الإصدار الأول؛ **البحث بالمحتوى مرحلة مستقلة لاحقة** |
| الحصص | حصة لكل مساحة + حد أقصى لحجم الملف، قابلة للضبط من الإدارة |
| الفيديو | بث HTTP Range مباشر دون transcoding |
| التخزين | SeaweedFS **موجود مسبقًا**؛ الربط عبر S3 API (endpoint وcredentials تُضبط لاحقًا في user-secrets/appsettings) |
| اسم الوحدة | تقنيًا `Drive`، وعربيًا «الملفات» (مفاتيح Ui labels تحت `drive.*`) |

## 2. الوضع الحالي (بعد مراجعة الكود)

| الموجود ونعيد استخدامه | غير الموجود ونبنيه |
| --- | --- |
| `StorageProviderType.AwsS3Id = 4` و`FileStorageProvider` (lookup المزوّدين) | لا يوجد تنفيذ S3 — `Storage/` فيه `LocalFileStorageService` فقط |
| `IFileStorageService` خاص بمرفقات الوثائق (مسار `{Year}/{Month}/{DocumentId}/...`) | تجريد تخزين جديد `IDriveStorageService` مستقل (لا نعدّل عقد المرفقات) |
| `Department.ManagerEmployeeId` + `User.EmployeeId` + `User.PrimaryDepartmentId` → اشتقاق رئيس قسم المستخدم | لا كيان «رئيس قسم» مستقل — نشتقه بالاستعلام نفسه |
| `BackgroundJobs/` (نمط `NotificationRetentionJob`) | مهمة تنظيف سلة Drive |
| Outbox + Notifications + `TamperEvidentAuditInterceptor` + `AuditActionMiddleware` | ربط أحداث Drive بها |
| `FileSignatureValidator` (التحقق من توقيعات الملفات) | إعادة استخدامه في رفع Drive |
| نمط الصلاحيات (`Permissions.All` + seed + مرآة `permissions.ts` + assertions) | صلاحييتان جديدتان |
| `InstitutionSetting` | إعدادات الحصص الافتراضية |
| كتالوج Ui labels + حارس lint | مفاتيح `drive.*` |

## 3. القرارات المطلوب اعتمادها (المقترح أولًا)

| # | القرار | المقترح | السبب |
| --- | --- | --- | --- |
| 1 | الصلاحيات | `DRIVE_USE` (USER + ADMIN) و`DRIVE_MANAGE` (ADMIN فقط: ضبط الحصص، الحذف النهائي، إدارة أي مساحة) | فصل الاستخدام عن الإدارة؛ مرآة في `permissions.ts` وتحديث assertions العدد |
| 2 | المعتمِد على الحذف | رئيس قسم **مالك العنصر**: للملفات الشخصية/مشتركة → `PrimaryDepartmentId` للمالك → `ManagerEmployeeId` → `User`. إن لم يوجد (بلا قسم/بلا رئيس/الرئيس غير نشط) → أي مستخدم بدور ADMIN (fallback) | اعتماد مؤسسي واضح بلا كيان جديد |
| 3 | الحذف المباشر | مسموح لـ: ADMIN، أو رئيس القسم على العناصر التي يملك عليها دور «إدارة» (مساحات يديرها أو عناصر مستخدمين من قسمه). ما عدا ذلك → طلب موافقة | يجمع «صلاحية الرئيس» و«طلب الموافقة» كما اعتُمد |
| 4 | الحذف النهائي من السلة | ADMIN أو رئيس القسم (بصلاحية إدارة على العنصر)؛ التنظيف التلقائي بعد 30 يومًا سياسة نظام تُسجَّل في التدقيق | شبكة أمان مزدوجة |
| 5 | عداد الحصة | `UsedBytes` على المساحة يتحدث ذرّيًا مع كل إصدار/حذف نهائي؛ **السلة تُحتسب ضمن الحصة** حتى الحذف النهائي (سلوك Drive) | منع التحايل بالحذف ثم الرفع |
| 6 | الإعدادات الافتراضية | حد الملف 200MB والحصة الافتراضية 5GB لكل مساحة، تُخزَّن في `InstitutionSetting` وتُعدَّل بـ `DRIVE_MANAGE`؛ حصة المساحة الخاصة تُجاوز الافتراضي | قابلة للضبط دون نشر كود |
| 7 | الرفع | Presigned PUT مباشر من المتصفح إلى SeaweedFS: `initiate` (حجز إصدار معلق + فحص الحصة/الحجم/الامتداد) → رفع → `complete` (تحقق الحجم والبصمة ثم اعتماد الإصدار). ملفات معلقة تُنظَّف بمهمة دورية | لا تمر البايتات عبر خادمنا مرتين؛ يفرض القيود قبل البايتات |
| 8 | مفتاح OnlyOffice | `DocumentKey = {itemId:N}-{versionNumber}` + JWT موقّع؛ جلسة تحرير لكل (مستخدم، ملف، إصدار)؛ الحفظ عبر callback ينشئ إصدارًا جديدًا ويعيد بناء المفتاح | قاعدة OnlyOffice: مفتاح فريد لكل نسخة، وإلا رفض التعاون |
| 9 | منع تضارب الحفظ | الإصدار الجديد من callback يُرفض إن تغيّر `CurrentVersionId` منذ فتح الجلسة (409) — باستثناء حالة forcesave الموثقة | منع lost-update |
| 10 | إزالة التكرار | إن كانت بصمة SHA-256 للمحتوى الجديد = بصمة الإصدار الحالي، لا يُنشئ إصدار (تُحدَّث الجلسة فقط) | حفظ OnlyOffice المتكرر بلا تغيير لا يلوّث السجل |
| 11 | الاسترجاع | استرجاع إصدار قديم = **نسخة جديدة** بمحتوى القديم (لا حذف ولا تعديل للإصدارات) | سجل غير قابل للتغيير |
| 12 | الأدوار والأنواع | ثوابت بنيوية في الدومين (ليست lookups قابلة للتحرير): kind (مجلد/ملف)، role (عرض/تحرير/إدارة)، حالة الطلب (معلق/موافق/مرفوض/ملغى)، مصدر الإصدار (رفع/حفظ محرر/استرجاع) | مفردات تقنية ثابتة؛ الترجمة عبر Ui labels |
| 13 | حدود المجلدات | عمق أقصى 30 مستوى، اسم ≤ 255 حرفًا، منع الأحرف `\ / : * ? " < > |`، فريد `(SpaceId, ParentId, Name)` بين غير المحذوفين | حماية الشجرة والاستعلامات |
| 14 | البحث بالاسم | يُطبَّق فحص الوصول **داخل الاستعلام** (مثل بحث الأرشيف §4.2 في خطة 07) لا بعد جلب الصفحة | صحة العدّ والترقيم |

## 4. الكيانات (Domain/Entities/Drive)

كلها بنمط `Entity<Guid>`/`AggregateRoot<Guid>` القائم، مع `RowVersion` للكيانات المتزامنة.

| الكيان | الحقول الأساسية | ملاحظات |
| --- | --- | --- |
| `DriveSpace` (AR) | `Name`, `Type` (Personal/Shared), `OwnerUserId`, `QuotaBytes` (null = الافتراضي), `UsedBytes`, `CreatedAt/By`, `TrashedAt` | الشخصية تُجهَّز تلقائيًا (lazy عند أول دخول أو مع إنشاء المستخدم — يُحسم في المرحلة 2)؛ فريد: شخصية واحدة لكل مستخدم |
| `DriveItem` (AR) | `SpaceId`, `ParentId` (null = جذر), `Kind`, `Name`, `OwnerUserId`, `CurrentVersionId?`, `SizeBytes` (للملف فقط = حجم الإصدار الحالي), `BreakInheritance`, `TrashedAt/ByUserId`, `CreatedAt/By`, `UpdatedAt` | عقدة موحّدة مجلد/ملف؛ نقل المجلد ينقل أبناءه ضمنيًا (لا تحديث لكل ابن)؛ `CurrentVersionId` يشير لأحدث إصدار معتمد |
| `DriveFileVersion` | `FileItemId`, `VersionNumber`, `StorageProviderId` (FK → `FileStorageProvider`), `ObjectKey`, `SizeBytes`, `ChecksumSha256`, `Source`, `CreatedByUserId`, `CreatedAt`, `Status` (Pending/Committed/Abandoned) | غير قابل للتعديل بعد الاعتماد؛ Pending للرفع المباشر؛ فهرس فريد `(FileItemId, VersionNumber)` |
| `DrivePermission` | `ItemId`, `PrincipalType` (User/Department), `PrincipalId`, `Role`, `GrantedByUserId`, `GrantedAt` | منح صريح فقط؛ الفعلي يُحسب بصعود الآباء؛ فريد `(ItemId, PrincipalType, PrincipalId)` |
| `DriveDeletionRequest` | `ItemId`, `RequestedByUserId`, `Reason` (≤500), `Status`, `ReviewedByUserId?`, `ReviewedAt?`, `ReviewNote?` (≤500), `CreatedAt` | طلب معلق واحد لكل عنصر (فهرس فريد مفلتر)؛ الموافقة تنقل للسلة ذرّيًا |
| `DriveEditSession` | `FileItemId`, `UserId`, `DocumentKey`, `BaseVersionId`, `Status` (Open/Saved/Closed/Expired), `OpenedAt`, `LastActivityAt` | تُغلق بمهمة دورية بعد خمول؛ مفتاح فريد نشط لكل (ملف، إصدار) |
| `DriveStar` | `UserId`, `ItemId`, `CreatedAt` | فريد `(UserId, ItemId)`؛ شخصي لا يُورَّث |

**أحداث الدومين:** `DriveItemTrashed`, `DriveItemRestored`, `DriveDeletionRequested/Approved/Rejected`, `DriveFileVersionCommitted`, `DriveSpaceCreated` → تُستهلك للإشعارات (Outbox) والتدقيق.

**أخطاء `Drive.*`:** `SpaceNotFound`, `ItemNotFound`, `NameInvalid`, `NameTaken`, `DepthLimitReached`, `QuotaExceeded`, `FileTooLarge`, `ExtensionNotAllowed`, `SignatureMismatch`, `AccessDenied`, `InheritanceBroken`, `DeletionPending`, `RequestNotFound`, `RequestAlreadyReviewed`, `VersionNotFound`, `VersionConflict`, `EditSessionExpired`, `CannotTrashRoot`, `CycleDetected` (نقل مجلد إلى داخل نفسه).

## 5. حساب الصلاحية الفعلية

```
Effective(user, item):
  1. ADMIN → Manage على كل شيء.
  2. مالك المساحة → Manage على مساحته (الشخصية أو المشتركة التي أنشأها).
  3. صعود من item إلى الجذر: عند كل عقدة، إن لم تكن BreakInheritance مكسورة فوقها،
     اجمع المنح الصريحة المطابقة (user مباشرة، أو قسم من أقسام المستخدم النشطة).
  4. أعلى دور مجموع = الدور الفعلي (Manage > Editor > Viewer).
  5. كسر الوراثة على عقدة: تُهمل منح ما فوقها وتُحسب منحها هي فقط (+ مالك المساحة/ADMIN).
```

- يُنفَّذ كخدمة `IDriveAccessEvaluator` في Application (بنمط `DocumentAccessEvaluator` الموجود) مع استعلام SQL يصعد الشجرة (recursive CTE) — **لا يُجلب كل صف ثم يُفحص**.
- قدرات الأدوار: **Viewer** تصفح/معاينة/تنزيل؛ **Editor** + رفع/تحرير/حفظ إصدار/إعادة تسمية/إنشاء مجلدات فرعية/نقل داخل المساحة؛ **Manage** + منح/سحب الصلاحيات وكسر الوراثة والنقل بين المساحات وطلبات الحذف المباشرة حيث تسري القاعدة §3.
- عناصر السلة: وصول المالك/ADMIN/رئيس القسم المعني فقط.

## 6. المسارات (مبدئية — تُفصَّل في المرحلة 4)

| المجموعة | المسارات | الصلاحية |
| --- | --- | --- |
| Spaces | `GET/POST /api/drive/spaces`, `GET/PUT /api/drive/spaces/{id}`, `PUT .../quota` | `DRIVE_USE`؛ الحصة `DRIVE_MANAGE` |
| Browse | `GET /api/drive/items?spaceId&parentId&search&kind&starred&trashed`, `GET /api/drive/items/{id}`, `GET .../breadcrumb`, `GET .../children` | `DRIVE_USE` + فحص وصول SQL |
| Items | `POST .../folders`, `PUT .../rename`, `PUT .../move`, `POST/DELETE .../star` | Viewer+ / Editor+ / Editor+ / شخصي |
| Upload | `POST /api/drive/files/initiate`, `POST /api/drive/files/{versionId}/complete`, `DELETE .../abandon` | Editor+ على الوجهة |
| Download/Preview | `GET /api/drive/files/{itemId}/content?versionId&disposition` (يدعم Range)، `GET .../download-url` (presigned قصير العمر) | Viewer+ |
| Versions | `GET /api/drive/files/{itemId}/versions`, `GET .../versions/{versionId}/content`, `POST .../versions/{versionId}/restore` | Viewer+ / Viewer+ / Editor+ |
| Permissions | `GET/POST/DELETE /api/drive/items/{id}/permissions`, `GET .../effective`, `PUT .../inheritance` | Manage |
| Deletion | `POST /api/drive/items/{id}/deletion-requests`, `GET /api/drive/deletion-requests?status`, `POST .../{reqId}/approve|reject|cancel`, `DELETE /api/drive/items/{id}` (حذف مباشر حيث يسري) | حسب §3 |
| Trash | `GET /api/drive/trash`, `POST /api/drive/trash/{itemId}/restore`, `DELETE /api/drive/trash/{itemId}` (نهائي) | حسب §3/§4 |
| OnlyOffice | `GET /api/drive/files/{itemId}/edit-config`, `POST /api/drive/onlyoffice/callback` (JWT، بلا جلسة مستخدم) | Editor+ / تحقق JWT |
| Settings | `GET/PUT /api/drive/settings` (الحدود الافتراضية) | `DRIVE_MANAGE` |

## 7. القواعد

### 7.1 الرفع والحصص
- `initiate`: فحص الصلاحية + الامتداد المسموح + الحجم ≤ الحد + الحصة المتبقية ≥ الحجم → إنشاء إصدار `Pending` بمفتاح S3 `{spaceId}/{itemId}/{versionId:N}` + presigned PUT (صلاحية 15 دقيقة).
- `complete`: تحقق الحجم الفعلي من S3 (HEAD) + بصمة SHA-256 (تُحسب دفقًا عند التنزيل الأول أو يرفعها العميل وتُتحقق) → `Committed` + تحديث `CurrentVersionId` و`UsedBytes` ذرّيًا داخل `TransactionPipelineBehavior`.
- مهمة دورية تلغي `Pending` الأقدم من ساعة وتحرر الحجز.
- قائمة امتدادات مسموحة قابلة للضبط (افتراضيًا: office/pdf/نصوص/صور/فيديو/مضغوطات) + `FileSignatureValidator` عند الاكتمال.

### 7.2 OnlyOffice
- `edit-config` يعيد: `documentUrl` (رابط تنزيل موقّع يصل إليه خادم OnlyOffice)، `key` (§3/8)، `callbackUrl`، JWT موقّع بمفتاح سري (user-secrets)، وضع التحرير حسب الدور (Viewer → معاينة فقط).
- callback: تحقق JWT → حالات `status=1` (تحرير جارٍ) / `2` (جاهز للحفظ: تنزيل الملف من `url` المرفق → إصدار جديد عبر نفس مسار الحفظ §7.1) / `4` (إغلاق بلا تغيير) / `6` (forcesave).
- رفض الحفظ إن تغيّر الإصدار الحالي منذ `BaseVersionId` (409 في رد callback وفق بروتوكول OnlyOffice) مع إشعار المستخدم.
- إزالة تكرار بالبصمة (§3/10). معاينة Word/Excel/PDF للقارئ تتم عبر OnlyOffice viewer أو معاينة PDF/صور مباشرة في الواجهة (يُحسم في خطة الواجهة).

### 7.3 الحذف والسلة
- الحذف المباشر (حيث يسري §3/3): نقل للسلة `TrashedAt` + حدث + تدقيق. المجلد يُسَلَّل دفعة واحدة (تحديث السلة على الجذر فقط، والأبناء يُستبعدون بالاستعلام `Any ancestor trashed`).
- طلب الموافقة: سبب إجباري ≤500، طلب معلق واحد للعنصر، إشعار للمعتمِد (§3/2) عبر Outbox؛ الموافقة → سلة + إشعار للطالب؛ الرفض/الإلغاء → إشعار.
- الاسترجاع: يعيد العنصر (وأبناءه الضمنيين) إن كان الأب غير محذوف، وإلا إلى جذر المساحة مع تنبيه.
- الحذف النهائي: ADMIN/رئيس القسم (§3/4) → حذف كائنات S3 لكل الإصدارات + الصفوف + تحرير `UsedBytes`، ذرّيًا.
- `DriveTrashCleanupJob` (نمط `NotificationRetentionJob`): نهائي تلقائي بعد 30 يومًا، يسجَّل كفاعل «النظام» في التدقيق.

### 7.4 التدقيق والإشعارات
- إضافة كيانات Drive إلى خريطة `TamperEvidentAuditInterceptor` (عدا `DriveStar` و`DriveEditSession` — تفضيل/حالة تقنية، تُسجَّل عبر تدقيق الإجراءات فقط).
- بادئات `AuditActionMiddleware`: `Drive*` / `Upload*` / `Restore*` / `Approve*` … تُراجع لتطابق أسماء الأوامر (درس المودول 07 §1/4).
- إشعارات: طلب حذف (للمعتمِد)، موافقة/رفض (للطالب)، مشاركة مساحة/منح صلاحية (للممنوح)، فشل حفظ تحرير (للمحرر).

### 7.5 البحث
- فلاتر: `search` (اسم، LIKE), `kind`, `extension`, `modifiedFrom/To`, `ownerUserId`, `starred`, `spaceId`, `parentId`. ترتيب افتراضي: الاسم تصاعديًا (المجلدات أولًا). ترقيم `PageRequest`.
- فحص الوصول داخل SQL (§3/14): عناصر المساحات التي يملكها المستخدم أو له منح عليها/على أصولها + عضوية أقسامه. يُعاد استخدام `IDriveAccessEvaluator` بصيغة قابلة للترجمة إلى SQL (predicate) كي لا ينحرف الفحصان.

## 8. المراحل

> كل مرحلة تنتهي ببوابات: `dotnet build` صفر أخطاء + `dotnet test` أخضر + README لكل مجلد ملامس (Contents + Change Log) + لا أسرار في الكود. لا تنفيذ قبل اعتماد الخطة، ولا انتقال بين المراحل قبل مراجعة المرحلة السابقة.

### المرحلة 0 — Domain (منفّذة 2026-11-19)
- الصلاحييتان `DRIVE_USE`/`DRIVE_MANAGE` في `Permissions.All` + `PermissionSeeder` + `RoleSeeder` + مرآة `permissions.ts` + تحديث assertions.
- الكيانات السبعة (§4) بأخطاء `Drive.*` وأحداث الدومين، ثوابت الأدوار/الأنواع/الحالات، عقود المستودعات (`IDriveSpaceRepository`, `IDriveItemRepository`, `IDriveFileVersionRepository`, `IDrivePermissionRepository`, `IDriveDeletionRequestRepository`, `IDriveEditSessionRepository`, `IDriveStarRepository`) وعقد `IDriveStorageService` (Application/Abstractions/Storage).
- اختبارات دومين: قواعد الاسم/العمق/الحصة/الإصدارات/حالات الطلب/النقل الدوري.

### المرحلة 1 — Infrastructure (التخزين والبنية) — ✅ منفّذة (2026-11-19)
- `S3DriveStorageService` (AWSSDK.S3 أو Minio client — يُحسم عند التنفيذ بما يتوافق مع SeaweedFS): presigned PUT/GET، HEAD، دفق Range، حذف، SHA-256 دفقًا. تسجيل مزوّد SeaweedFS في `FileStorageProviderSeeder` (النوع `AwsS3Id`) بقيم من الإعدادات/user-secrets — **لا أسرار في الكود**.
- EF configurations + migration `Add_Drive_Module`: الجداول السبعة، FKs (بما فيها `StorageProviderId` و`CurrentVersionId` و`ParentId` self-FK)، الفهارس الفريدة والمفلترة (§4)، `RowVersion`.
- تنفيذ المستودعات + `DriveAccessEvaluator` (recursive CTE) + تسجيل DI.
- خريطة التدقيق + بادئات middleware + أحداث Outbox للإشعارات.

### المرحلة 2 — Application (النواة) — ✅ منفّذة (2026-11-19)
- `Features/Drive/`: Spaces (إنشاء/عرض/تعديل/حصة + تجهيز الشخصية تلقائيًا)، Items (مجلد/إعادة تسمية/نقل مع فحص الدورة/تصفح/سلسلة مسار)، Stars، Upload (initiate/complete/abandon)، Download/Range، Versions (قائمة/تنزيل/استرجاع)، Permissions (منح/سحب/فعلي/كسر وراثة)، DeletionRequests + الحذف المباشر، Trash (قائمة/استرجاع/نهائي)، Search بالاسم، Settings.
- Validators لكل أمر + أخطاء موحدة + اختبارات handlers (بما فيها الحصص المتزامنة، كسر الوراثة، الحذف المباشر مقابل الطلب، fallback المعتمِد ADMIN).

### المرحلة 3 — Server (النواة) — ✅ منفّذة (2026-11-19)
- Controllers + Contracts لكل مسارات §6 (عدا OnlyOffice)، سمات الصلاحيات، حدود حجم الطلب، OpenAPI، تسجيل المهام الدورية (تنظيف السلة + Pending + الجلسات الخاملة).
- تحقق حي على API التطوير بمؤشرات نجاح/فشل لكل مسار (نمط خطط 07/06).
- **المنفّذ:** سبعة متحكمات (`DriveSpaces/Items/Files/Permissions/Deletion/Trash/Settings`) + `Contracts/Drive/DriveRequests.cs`. البوابات خضراء (بناء 0 أخطاء، 1223 اختبارًا). تحقق حي على المنفذ 5028 بحساب `superadmin`: كل مسارات البيانات الوصفية 200 (مساحات/عناصر/صلاحيات/حذف/سلة/إعدادات)، بوابة المصادقة 401، والتحقق 400 (`Drive.ExtensionsEmpty`)، و`Drive.NotAFile` على مجلد. مسارات بايتات الملف (initiate/content/download-url/versions/restore) تنتظر مفاتيح SeaweedFS (`Drive.StorageNotConfigured` 500 متوقع). معالجة Range يدوية (206 + Content-Range). المهام الدورية مسجّلة منذ المرحلة 1. مسارات OnlyOffice مؤجلة للمرحلة 4.

### المرحلة 4 — OnlyOffice (تحرير ومعاينة) — ✅ منفّذة (2026-11-19)
- `DriveEditSession` endpoints: `edit-config` (JWT) + `callback` (تحقق JWT، حفظ → إصدار جديد، تعارض 409، forcesave، إغلاق).
- إعدادات الاتصال (عنوان الخادم، السر، callback) في appsettings/user-secrets + وثيقة تشغيل: نشر OnlyOffice Document Server (Docker) وربطه بشبكة الخادم وSeaweedFS.
- اختبارات callback (كل الحالات + توقيع خاطئ + تعارض إصدار + بصمة مكررة) + تحقق حي بجلسة تحرير فعلية على ملف Word وExcel.
- **المنفّذ:** `GetDriveEditConfigQuery` (إعداد المحرر الموقّع HS256، وضع edit/view حسب الدور، رابط تنزيل موقّع، مفتاح `{itemId:N}-{versionNumber}`، جلسة تحرير واحدة مفتوحة لكل مفتاح يعيد استخدامها المتعاونون) و`ProcessDriveOnlyOfficeCallbackCommand` (تحقق JWT بلا جلسة مستخدم؛ الفاعل = مالك الجلسة؛ status 1 لمس/4 إغلاق/2+6 حفظ؛ رفض التعارض `CurrentVersionId ≠ BaseVersionId` مع إشعار المحرر `DriveEditSaveFailed`؛ إزالة التكرار ببصمة SHA-256؛ حراسة الحجم/الحصة/التوقيع؛ `WriteAsync` خادمي ثم إصدار جديد `Source=EditorSave`). بنية OnlyOffice في Infrastructure (`DriveOnlyOfficeOptions`/`OnlyOfficeTokenService`/`DriveCallbackDownloader`) + `IDriveStorageService.WriteAsync` + قسم `Drive:OnlyOffice` في appsettings (السر عبر user-secrets). متحكم `DriveOnlyOfficeController` (edit-config بـ`DRIVE_USE`؛ callback بـ`[AllowAnonymous]` يعيد دومًا HTTP 200 `{error:0|1}`). وثيقة التشغيل `Infrastructure/OnlyOffice/README.md` (Docker + السر + قابلية الوصول بأربع legs). **قراران موثّقان للمراجعة:** (1) فحص التعارض يُطبَّق على forcesave أيضًا منعًا لـlost-update (استثناء forcesave في §3/9 اختير عدم تفعيله حفاظًا على البيانات)؛ (2) نتيجة الأمر `Result.Success` دومًا (خطأ البروتوكول في الحمولة) كي تلتزم المعاملة ويُحفَظ إشعار فشل الحفظ رغم الرفض. 27 اختبارًا جديدًا (handler + token)؛ البوابات خضراء (بناء 0 أخطاء، 1250 اختبارًا). **التحقق الحي الجزئي** (بلا OnlyOffice/SeaweedFS): edit-config يعيد 500 `Drive.OnlyOfficeNotConfigured` عند غياب الإعداد، وcallback يعيد 200 `{error:1}` لتوقيع خاطئ. جلسة التحرير الفعلية على Word/Excel تنتظر عنوان OnlyOffice + السر + مفاتيح SeaweedFS.

### المرحلة 5 — الاختبارات الشاملة والبوابات النهائية — ✅ منفّذة (2026-11-20)
- اختبارات تكامل: سيناريو كامل (مساحة → مجلدات → رفع → صلاحيات موروثة/مكسورة → تحرير → إصدارات → طلب حذف → موافقة → سلة → استرجاع → نهائي → حصة).
- اختبار حمل خفيف متزامن على عداد الحصة والإصدارات.
- مراجعة أمنية: مسارات presigned (عمر/نطاق)، JWT، حقن الأسماء، اجتياز المسار، فحص التوقيعات، صلاحيات السلة.
- تحديث README الجذر + خطة الواجهة المنفصلة + مفاتيح `drive.*` في كتالوج Ui labels وحارس lint.
- **المنفّذ:** `DriveLifecycleIntegrationTests` (سيناريو متصل واحد عبر مستودعات SQLite حقيقية + المقيّم الحقيقي + S3 في الذاكرة: مساحة «مساحة التكامل» → مجلدان → رفع «تقرير.docx» v1 → منح Editor موروث ثم كسر الوراثة ثم Viewer مباشر → edit-config/callback حفظ v2 `EditorSave` → استرجاع v1 كـ v3 `Restore` → طلب حذف يُشعر الرئيس → موافقة الرئيس → سلة (تصفح حي يستبعد/سلة يشمل) → استرجاع → طلب+موافقة ثانية → حذف نهائي بـ ADMIN يزيل الكائنات والإصدارات → الحصة 0). `DriveConcurrencyTests` (3): إعادة فحص الحصة عند complete تمنع التجاوز رغم نجاح initiateَين متزامنين؛ دفعة 20 حفظ محرر تُنتج 21 إصدارًا مرقّمة 1..21 بحصة مضبوطة؛ فهرس `UX_DRIVE_VERSIONS_NO` الفريد يرفض التخصيص المتزامن المكرر (`DbUpdateException` عبر سياق ثانٍ على نفس الاتصال). `DriveSecurityTests` (9): رفض اجتياز الاسم/المسار (`../../evil.docx`، `a/b`، `a\b`، `..`)، الغريب لا ينال بايتات/رابط/إصدارات، عزل إصدار ملف آخر ورفض الإصدار المعلّق، Viewer يقرأ ولا يعدّل. **إصلاح إنتاجي حقيقي** كشفه اختبار التكامل: `DrivePurgeService` كان يحذف جلسات التحرير بـ`ExecuteDeleteAsync` متجاوزًا متتبّع التغيير، فتبقى جلسة متتبَّعة كتابع حيّ ويرمي `RemoveRange(versions)` على علاقة FK مقطوعة؛ الآن تُحمَّل الجلسات وتُزال عبر المتتبّع قبل حذف الإصدارات (+ اختبار انحدار `Purge_WithTrackedEditSession_RemovesSessionAndVersions`). **المراجعة الأمنية** أكّدت: روابط presigned مقصورة على bucket+key+verb واحد بأعمار قصيرة (رفع 15د/تنزيل 5د/رابط مستند المحرر 15د) ومفاتيح الكائنات GUID لا تحمل اسم المستخدم؛ callback بـ JWT HS256 بلا جلسة مستخدم؛ `ValidateName`/`ValidateKey` يرفضان الحقن والاجتياز؛ `DriveVersionResolver` يحرس القراءة بدور Viewer ويعزل الملفات ويرفض البايتات المعلّقة؛ السلة/الحذف النهائي حصري بـ ADMIN أو رئيس+Manage. **الوثائق:** README الجذر (صف المودول 11 + سجل تغيير)، `Features/Drive`، `Infrastructure/Storage` (إصلاح الحذف)، `Tests`، وكتالوج Ui labels (`drive.ts` بـ 56 مفتاحًا مرآة لـ`DriveCatalog.cs` + حارس lint؛ تحذيرات يتيم متوقعة حتى وصول الواجهة). البوابات: بناء 0 أخطاء + 1264 اختبارًا؛ lint/build الواجهة خضراء. لا أسرار في الكود؛ لا commits.

## 9. خارج النطاق (مراحل/خطط مستقلة)

1. **البحث بالمحتوى**: استخراج نصوص Word/Excel/PDF + فهرسة (OpenSearch أو SQL FTS) + مسار معالجة غير متزامن — خطة مستقلة بعد استقرار النواة.
2. **واجهة Drive** (Google Drive UX): خطة `module-11-drive-frontend.md` — شريط جانبي (ملفاتي/المشتركة/المفضلة/السلة)، شبكة/قائمة، breadcrumb، سحب وإفلات، معاينة، محرر مضمّن، لوحة إصدارات، حوار صلاحيات، عداد حصة.
3. Transcoding فيديو، روابط مشاركة خارجية، multipart upload للملفات >2GB، فحص فيروسات، مزامنة عميل سطح مكتب.

## 10. مخاطر وملاحظات تشغيلية

| الخطر | التخفيف |
| --- | --- |
| حد 20 جلسة OnlyOffice متزامنة | مراقبة عدد الجلسات النشطة؛ ترقية الترخيص لاحقًا إن لزم (لا تغيير كود) |
| SeaweedFS غير متاح | أخطاء واضحة + إعادة محاولة توليد presigned؛ لا فقدان بيانات لأن الإصدار لا يُعتمد إلا بعد التحقق |
| تضارب حفظ متزامن | قاعدة المفتاح + فحص الإصدار (§7.2) + إشعار الفشل |
| نمو الإصدارات بلا حد | سياسة احتفاظ اختيارية لاحقًا (خارج النطاق الآن) — تُبنى على `Source`/`CreatedAt` الجاهزين |
| تفاصيل اتصال SeaweedFS وOnlyOffice غير معروفة بعد | تُطلب قبل المرحلة 1 (S3 endpoint/bucket/keys) وقبل المرحلة 4 (عنوان OnlyOffice) |

## Change Log

| التاريخ | المؤلف | التغيير |
| --- | --- | --- |
| 2026-11-20 | Qoder | تنفيذ المرحلة 5 (الاختبارات الشاملة والبوابات النهائية) وإغلاق المودول في الباك إند: `DriveLifecycleIntegrationTests` (سيناريو متصل كامل مساحة→مجلدات→رفع→صلاحيات موروثة/مكسورة→تحرير→إصدارات→طلب حذف→موافقة→سلة→استرجاع→نهائي→حصة)، `DriveConcurrencyTests` (إعادة فحص الحصة عند complete، دفعة 20 حفظ محرر بإصدارات فريدة متسلسلة، فهرس رقم الإصدار الفريد يرفض التكرار المتزامن)، `DriveSecurityTests` (اجتياز الاسم/المسار، قراءات الغريب، عزل ملف آخر/إصدار معلّق، Viewer يقرأ لا يعدّل). إصلاح إنتاجي حقيقي كشفه التكامل: `DrivePurgeService` يزيل جلسات التحرير عبر متتبّع التغيير لا `ExecuteDeleteAsync` (+ اختبار انحدار). مراجعة أمنية أكّدت عمر/نطاق presigned وJWT callback وحقن الأسماء والاجتياز وعزل الإصدارات وصلاحيات السلة. 14 اختبارًا جديدًا (المجموع 1264، بناء 0 أخطاء). تحديث README الجذر + `Features/Drive` + `Infrastructure/Storage` + `Tests` + كتالوج Ui labels (`drive.ts` بـ 56 مفتاحًا `drive.*` مرآة لـ`DriveCatalog.cs`، حارس lint exit 0 مع تحذيرات يتيم متوقعة حتى وصول الواجهة المنفصلة `module-11-drive-frontend.md`)؛ lint/build الواجهة خضراء. لا أسرار في الكود؛ لا commits. |
| 2026-11-19 | Qoder | تنفيذ المرحلة 4 (OnlyOffice): `GetDriveEditConfigQuery` + `ProcessDriveOnlyOfficeCallbackCommand` (حفظ → إصدار `EditorSave`، رفض تعارض + إشعار المحرر، إزالة تكرار SHA-256، حراسة حجم/حصة/توقيع، `WriteAsync` خادمي)، بنية Infrastructure (`DriveOnlyOfficeOptions`/`OnlyOfficeTokenService` HS256/`DriveCallbackDownloader`) + `IDriveStorageService.WriteAsync` + قسم `Drive:OnlyOffice` (السر عبر user-secrets)، متحكم `DriveOnlyOfficeController` (callback بـ`[AllowAnonymous]` يعيد دومًا 200 `{error:0\|1}`)، وثيقة تشغيل `Infrastructure/OnlyOffice/README.md`. قراران للمراجعة: فحص التعارض يشمل forcesave؛ نتيجة الأمر Success دومًا ليلتزم إشعار الفشل. 27 اختبارًا جديدًا (المجموع 1250، بناء 0 أخطاء). تحقق حي جزئي (edit-config → 500 `OnlyOfficeNotConfigured`؛ callback توقيع خاطئ → 200 `{error:1}`)؛ جلسة Word/Excel الفعلية تنتظر عنوان OnlyOffice + مفاتيح SeaweedFS. تحديث READMEs (Features/Drive، Controllers، Contracts، Storage، OnlyOffice الجديد، Infrastructure، Server). لا أسرار في الكود؛ لا commits. |
| 2026-11-19 | Qoder | تنفيذ المرحلة 3 (Server): سبعة متحكمات نحيفة لكل مسارات §6 عدا OnlyOffice (`DriveSpaces/Items/Files/Permissions/Deletion/Trash/Settings`) + `Contracts/Drive/DriveRequests.cs`، بسمات `[EndpointSummary]/[EndpointDescription]` ووصف كل معلمة (اختبارات OpenAPI خضراء). تعزيز `DriveFileContentResult` بـ`FileName` وحدود Range الفعلية؛ معالجة Range يدوية في المتحكم (206 + `Content-Range` + `Accept-Ranges`). البوابات: بناء 0 أخطاء + 1223 اختبارًا. تحقق حي على المنفذ 5028 (حساب `superadmin`): 34+ استدعاءً ناجحًا لكل مسارات البيانات الوصفية، 401 بلا رمز، 400 تحقق، و`Drive.NotAFile`/`Drive.StorageNotConfigured` متوقعة لمسارات البايتات (تنتظر مفاتيح SeaweedFS). تحديث READMEs (Controllers/Contracts/Features/Drive). لا أسرار في الكود؛ لا commits. |
| 2026-11-19 | Qoder | تنفيذ المرحلة 2 (Application): نواة `Features/Drive/` كاملة — Spaces/Items/Stars/Upload(initiate/complete/abandon)/Download+Range/Versions+Restore/Permissions+كسر الوراثة/DeletionRequests+الحذف المباشر/Trash+Purge/Search بالاسم/Settings، مع Validators وأخطاء `Drive.*` موحدة و`DriveUploadGuard` و`DriveNotificationPublisher`. عقود مضافة: `IDriveStorageService.CopyAsync` و`IDrivePurgeService` (مشاركة بين الحذف اليدوي ومهمة الصيانة) ومرشّح `reviewerUserId` لصندوق طلبات الحذف. 41 اختبار handler جديدًا عبر `DriveHandlerWorld` (المجموع 1223). إصلاح دورة FK في الحذف النهائي (`CurrentVersionId` Restrict مقابل Cascade). تحديث READMEs الملامسة. لا أسرار في الكود. |
| 2026-11-19 | Qoder | تنفيذ المرحلة 1 (Infrastructure): `S3DriveStorageService` + `DriveStorageOptions` (AWSSDK.S3 4.1.2، path-style)، مزوّد SeaweedFS في البذر، `DriveConfigurations.cs` + migration `Add_Drive_Module` (7 جداول + أعمدة الحصص)، المستودعات السبعة + `DriveTreeQuery` (CTE) + `DriveItemAccessQuery` (مرآة SQL) + `DriveAccessEvaluator`، خريطة التدقيق + بادئات middleware + أنواع إشعارات Outbox، `DriveMaintenanceJob`، وقسم `Drive` في appsettings. 52 اختبارًا جديدًا (المجموع 1182). تحديث 14 README. لا أسرار في الكود؛ مفاتيح SeaweedFS عبر user-secrets مطلوبة للتشغيل الحي. |
| 2026-11-19 | Qoder | تنفيذ المرحلة 0: الصلايتان `DRIVE_USE`/`DRIVE_MANAGE` (25 رمزًا، 28 منحة دور)، الكيانات السبعة + `DriveCatalog.cs` + `DomainErrors.Drive` + 7 أحداث + `IDriveRepositories.cs` + `IDriveStorageService`، و66 اختبار دومين (المجموع 1130). تحديث READMEs الملامسة وknowledge.md ومرآة permissions.ts. |
| 2026-11-19 | Qoder | إنشاء الخطة بعد اعتماد قرارات النقاش (OnlyOffice، مساحات شخصية+مشتركة، وراثة صلاحيات، حذف بموافقة رئيس القسم حتى الشخصية، إصدارات بلا حد، سلة 30 يومًا، حصص، بحث الاسم أولًا والمحتوى لاحقًا، SeaweedFS عبر S3). |