# خطة الوحدة 10 — الإشعارات (الواجهة الأمامية)

> **الحالة: منفّذة (2026-11-04) — المرحلة ب (الخلفية) والمراحل 0–5 منجزة؛ `dotnet test` 743 ناجح، و`npm run build` و`npm run lint` ناجحان، وجُرّب الدفع الحي وقراءة/إخفاء الإشعار وانقطاع الاتصال على API حقيقي. بقيت المرحلة 6 اليدوية جزئيًا (انظر القسم 9).** ~~مسودة للمراجعة النهائية~~ الواجهة الخلفية منفّذة ومتحقَّق منها على API حي (`plans/module-10-notifications-backend.md`، القسم 8). **تتضمن الخطة إضافتين صغيرتين في الخلفية (المرحلة ب) تسبقان الواجهة**، وقرارًا اختياريًا عن أنواع إشعار إضافية (القسم 7).

## 1. الهدف

إظهار إشعارات النظام الداخلية للمستخدم **لحظيًا وبشكل موثوق**:

- **جرس** في الشريط العلوي بعدّاد غير المقروء وقائمة منسدلة بآخر الإشعارات.
- **تنبيه فوري (toast)** عند وصول إشعار جديد عبر SignalR، يفتح الوثيقة المعنية بنقرة.
- صفحة **`/notifications`**: صندوق وارد كامل (تصفية بالنوع وغير المقروء، ترقيم، قراءة، قراءة الكل، إخفاء).
- اتصال حي **يعيد الاتصال تلقائيًا** ويتراجع إلى استعلام دوري عند الانقطاع، دون إشعارات مكرّرة.

## 2. الحالة الراهنة

| العنصر | الحالة |
| --- | --- |
| API | ✅ `GET /api/notifications` (`unreadOnly`, `typeId`, `page`, `pageSize` ≤ 100)، `GET /unread-count`، `POST /{recipientId}/read`، `POST /read-all`، `POST /{recipientId}/dismiss` |
| ما ينقص API | ❌ لا `search` ولا `sortBy` في القائمة (الترتيب ثابت بـ `DeliveredAt` تنازليًا)، و`unread-count` يعيد الإجمالي فقط بلا تفصيل بالنوع ⇒ تُسدّ في **المرحلة ب** |
| الـ Hub | ✅ `/hubs/notifications` (SignalR JSON)، التوكن عبر `access_token`، الحدث `notificationReceived`، يُغلق الاتصال عند انتهاء التوكن |
| الصلاحيات | لا رمز صلاحية: أي توكن صالح، ومحصور بصاحبه (صف غيره ⇒ `404 Notifications.RecipientNotFound`). تبقى الصلاحيات **17** |
| وكيل Vite | ✅ `/hubs` مع `ws: true` في `vite.config.ts` |
| قوائم المراجع | ✅ `notification-types` و`notification-priorities` في الخادم، غير مسجّلة في `LOOKUP_SLUGS` |
| مكتبة SignalR | ❌ غير مثبّتة (`@microsoft/signalr`) |
| جرس / صفحة / عميل Hub / كتالوج أخطاء / أدلة تعليمية | ❌ غير موجودة |
| مسار `ROUTES.NOTIFICATIONS` وعنصر القائمة الجانبية | ❌ غير موجودان |

## 3. الفجوات المطلوب سدّها

0. **الخلفية:** بحث وترتيب في قائمة الإشعارات، وتفصيل العدّاد بالنوع (المرحلة ب).
1. مكتبة SignalR وعميل اتصال يُحمَّل كسولًا بعد تسجيل الدخول.
2. طبقة قراءة (أنواع، استعلامات، عدّاد) وطبقة كتابة (قراءة، قراءة الكل، إخفاء).
3. ربط الدفع الحي بذاكرة React Query (الدفع **تنبيه فقط**، قاعدة البيانات هي المصدر).
4. الجرس والقائمة المنسدلة وصفحة صندوق الوارد.
5. رسائل خطأ عربية، وأدلة تعليمية، وتحديث الوثائق.

## 4. القرارات المعمارية (FSD الصارم)

| الطبقة | الشريحة | المحتوى |
| --- | --- | --- |
| `shared/config` | `lookupIds.ts` | `NOTIFICATION_TYPE` (1–11) و`NOTIFICATION_PRIORITY` (1 عادية، 2 عالية، 3 عاجلة) بقيم الخادم حرفيًا؛ `routes.ts`: `NOTIFICATIONS: '/notifications'` |
| `shared/config/guides` | `notifications.ts` | كتالوج الأدلة + تسجيله في `index.ts` |
| `shared/api` | `errorMessages.ts` | مفاتيح `Notifications.*` بالعربية |
| `shared/lib` | `safeRelativeUrl.ts` | يقبل `actionUrl` فقط إن بدأ بـ `/` واحدة بلا مخطط ولا مضيف (دفاع إضافي لما يتحقق منه الخادم) |
| `entities` | `notification` | الأنواع، `notificationKeys`، `useNotifications`، `useUnreadCount`، `NotificationTypeIcon`، `NotificationPriorityBadge`، `NotificationItem` (عرض صف واحد) |
| `entities/lookup` | `constants.ts` | `LOOKUP_SLUGS.NotificationTypes` و`NotificationPriorities` + `LOOKUP_FALLBACKS` عربية |
| `features` | `notification-manage` | `useMarkRead`، `useMarkAllRead`، `useDismiss` (مع تحديث تفاؤلي للعدّاد) |
| `features` | `notification-live` | `useNotificationHub`: الاتصال، إعادة الاتصال، إزالة التكرار، التنبيهات، الاستعلام الاحتياطي |
| `widgets` | `notification-bell` | الجرس + العدّاد + القائمة المنسدلة + حالة الاتصال |
| `widgets` | `notification-inbox` | جدول/قائمة صندوق الوارد الكامل بمرشحاته وقائمته السياقية |
| `pages` | `notifications` | صفحة `/notifications` (≤ ~120 سطرًا) |
| `app/providers` | `NotificationLiveProvider` | يشغّل الاتصال طالما الجلسة مصادَق عليها، ويوقفه عند الخروج |
| `app/router` | `AppRouter` | مسار `/notifications` + تمرير الجرس لقالب الصفحة |

قرارات تفصيلية:

- **من يملك الاتصال؟** الاتصال ظاهرة عامة للجلسة لا لصفحة، فيُركَّب مرة واحدة في `app/providers` عبر `NotificationLiveProvider` داخل `QueryProvider` وبعد `AuthSessionLoader`. منطقه في `features/notification-live` (يستورد مفاتيح `entities/notification` فقط عبر الـ barrel)، والمزوّد في طبقة `app` المسموح لها باستيراد الـ features.
- **الجرس داخل الشريط العلوي دون كسر FSD:** `widgets/app-shell` لا يستورد widget شقيقًا. يقبل `AppShell` خاصية `headerSlot?: ReactNode` (أو `headerActions`) ويمرّر `AppRouter` (طبقة `app`) العنصر `<NotificationBell />`. ويُضاف عنصر «الإشعارات» إلى `shell-nav.ts` (مجموعة «نظرة عامة») بلا `permission` لأنه لا صلاحية له.
- **الاتصال:** `@microsoft/signalr` يُستورد بـ `import()` ديناميكي عند أول تسجيل دخول لئلا يدخل الحزمة الأساسية. `withUrl('/hubs/notifications', { accessTokenFactory: () => tokenStorage.get() ?? '' })` (يرسله الخادم `access_token` في سلسلة الاستعلام للـ WebSocket)، و`withAutomaticReconnect([0, 2000, 10000, 30000])`، ومستوى سجل `Warning`.
- **انتهاء التوكن:** الخادم يغلق الاتصال عند انتهاء التوكن. عند `onclose` النهائي: إن لم يعد التوكن موجودًا (سجّل خروجًا أو رد 401 مسح الجلسة) لا نعيد المحاولة؛ وإلا نحاول إعادة بناء الاتصال بمهلة متصاعدة. أي 401 يعالجه `httpClient` أصلًا (مسح الجلسة والتحويل إلى الدخول).
- **الدفع تنبيه فقط:** عند `notificationReceived` وعند `onreconnected` وعند استعادة التركيز بعد انقطاع: `invalidateQueries(notificationKeys.all(userId))` (القائمة والعدّاد معًا). لا نكتب حمولة الدفع في الذاكرة المؤقتة.
- **إزالة التكرار:** مجموعة محدودة (آخر 200) من `notificationId` المعروضة كتنبيه؛ تتجاهل التنبيه إن رُئي من قبل (إعادة تسليم الـ outbox). تكرار التبويبات يُقبل (كل تبويب اتصاله وتنبيهه).
- **التنبيه الفوري:** `sonner` بعنوان `titleAr` ونص `bodyAr` وزر «فتح» يستدعي `navigate(safeRelativeUrl(actionUrl))` ويعلّم الصف مقروءًا. `priorityId = 3` (عاجلة) يبقى حتى الإغلاق اليدوي؛ البقية 6 ثوانٍ. لا صوت ولا إشعارات المتصفح (خارج النطاق).
- **الاستعلام الاحتياطي:** عندما `connectionState !== Connected` يعمل `useUnreadCount` بـ `refetchInterval: 60_000`، وعند الاتصال يتوقف الاستعلام الدوري (`false`)، ويبقى `refetchOnWindowFocus`. العدّاد غير مدقَّق في الخادم فآمن للاستعلام.
- **مفاتيح الاستعلام تحمل `userId`:** `notificationKeys.all(userId)` من `useSessionStore` حتى لا يرى مستخدم لاحق على نفس المتصفح ذاكرة غيره؛ ويُزال كل شيء بـ `removeQueries` عند الخروج.
- **`recipientId` لا `notificationId`:** كل عمليات القراءة/الإخفاء تستخدم `recipientId`؛ و`notificationId` للإزالة من التكرار فقط.
- **التحديث التفاؤلي:** `useMarkRead` و`useDismiss` يعدّلان العدّاد والصف في الذاكرة فورًا مع تراجع عند الخطأ؛ العمليات idempotent في الخادم. `read-all` يعيد `{ count }` ويبطل المفاتيح فقط.
- **صفحة `/notifications`:** القراءة من الخادم بترقيم (حتى 100) وبحث وترتيب **من الخادم** بعد المرحلة ب، والحالة كلها في الـ URL: `?unread=1&type=&search=&sortBy=&desc=&page=&pageSize=` (الافتراضي: الأحدث أولًا). البحث debounced عبر `useDebouncedValue` ويطابق العنوان والنص. الأعمدة القابلة للترتيب: التاريخ، الأولوية، النوع، حالة القراءة. شارات على مرشح النوع من `byType`. مُختار الأعمدة محفوظ بالمفتاح `diwan.table.notifications.columns.v1`. الحالات: تحميل (هيكل) / فارغ (نصان: لا إشعارات، لا غير مقروء) / خطأ + إعادة محاولة. لا حالة «ممنوع» لأن لا صلاحية.
- **القائمة السياقية (وبـ `Shift+F10`):** فتح الوثيقة، تعليم كمقروء، إخفاء، نسخ الرابط. لا «تعليم كغير مقروء» (غير مدعوم في API).
- **النقر على صف/إشعار:** يعلّمه مقروءًا ثم يفتح `actionUrl` إن وُجد وصحّ؛ ولو رُفض فتح الوثيقة لاحقًا بـ `Correspondence.DocumentAccessDenied` تعالجه صفحة التفاصيل أصلًا.
- **الأولويات:** `NotificationPriorityBadge` — العاجلة بلون الخطأ الدلالي، العالية بلون التحذير؛ ألوان من `tokens.css` فقط.
- **العربية أولًا:** CSS منطقي، لا hex، الأرقام الرسمية والتواريخ ISO في جزر `dir="ltr"`، أيقونة الجرس لا تُعكس، وتنطق قارئات الشاشة العدّاد (`aria-label="الإشعارات، 3 غير مقروءة"`) ومنطقة `aria-live="polite"` للتنبيهات.

## 5. المراحل

### المرحلة ب — إضافات الخلفية (تسبق كل مراحل الواجهة)

إضافات **تراجعية متوافقة** (additive): لا تغيير في الجداول ولا ترحيل ولا أنواع صلاحيات جديدة، والمستهلكون الحاليون لا يتأثرون.

**ب1 — `search` و`sortBy` في `GET /api/notifications`**
- معاملات جديدة اختيارية: `search` (حتى 100 حرف، يطابق `TitleAr` و`BodyAr` بـ `LIKE` مع تهريب `%` و`_` و`[`)، `sortBy` (`deliveredAt` افتراضيًا، `priority`، `type`، `isRead`)، `sortDescending` (افتراضيًا `true`). ترتيب ثانوي ثابت بـ `RecipientId` كما هو الآن. قيمة `sortBy` غير معروفة تتبع أسلوب بقية القوائم (مثل المستخدمين): تعود للافتراضي أو رمز خطأ تحقق موثّق — يُحسم بقراءة `ListUsersQuery` عند التنفيذ.
- **الملفات:** `ListNotificationsQuery.cs` (حقول جديدة بقيم افتراضية فلا ينكسر استدعاء قائم)، `ListNotificationsQueryHandler.cs` (تقليم `search` وتمريره)، `INotificationRepository.GetInboxAsync` في `IDomainRepositories.cs` (معامل `NotificationInboxFilter`/حقول جديدة بدل زيادة معاملات موضعية، وتحديث الاستدعاءات الأربعة في `NotificationsTests.cs`)، `NotificationRepository.cs` (تطبيق البحث والترتيب قبل `Skip/Take`)، `NotificationsController.cs` (معاملات + `Description` بمثال لكل معامل + تحديث ملخص/وصف المسار؛ `OpenApiDocumentationTests` يفشل البناء إن نقص أي منها).
- **القبول:** بحث بعنوان/نص عربي يعيد صفوف المستخدم وحده؛ `search` مع `unreadOnly` و`typeId` يعملان معًا والعدّ الكلي صحيح؛ كل مفتاح ترتيب يعمل تصاعديًا وتنازليًا بثبات بين الصفحات.

**ب2 — تفصيل العدّاد بالنوع في `GET /api/notifications/unread-count`**
- `UnreadCountResponse` يصبح `{ count, byType: [{ typeId, count }] }` (إضافة حقل فقط). يبقى المسار غير مدقَّق (`[SkipActionAudit]`) وآمنًا للاستعلام الدوري. يُحسب بـ `GROUP BY` واحد على الصفوف غير المقروءة وغير المخفية.
- **الملفات:** `GetUnreadCountQuery`/handler/`UnreadCountResponse`، وطريقة جديدة في `INotificationRepository` (`CountUnreadByTypeAsync`) إلى جانب `CountUnreadAsync`، والمتحكم والتوثيق.
- **القبول:** مجموع `byType` يساوي `count`؛ صفر لا يظهر في المصفوفة؛ صف مقروء أو مخفي لا يُحتسب.

**ب3 — الاختبارات والوثائق**
- اختبارات Persistence جديدة في `NotificationsTests.cs` (بحث، تهريب الرموز الخاصة، كل مفاتيح الترتيب، ثبات الترقيم، `byType`)، وتحديث اختبار OpenAPI تلقائيًا بالتوثيق.
- README المجلدات التي تُلمَس (`Features/Notifications`, `Server/Controllers`, `Infrastructure/Persistence/Repositories`, `Tests/Persistence`) بـ Contents/Status/Change Log، وجدول مسارات الإشعارات في `knowledge.md`، وسجل README الجذر، وقسم 8 في الخطة الخلفية («ما أُضيف بعد التنفيذ»).
- **البوابة:** `dotnet build InstitutionalDiwan.slnx` و`dotnet test InstitutionalDiwan.slnx` نظيفان (عدد الاختبارات يرتفع عن 740)، وتجربة سريعة على API حي قبل الانتقال للواجهة.

### المرحلة 0 — الأساس
- `npm install @microsoft/signalr` (أحدث إصدار مستقر؛ يُثبَّت بعد التأكد من توافقه مع .NET 10 على الـ hub الحي).
- `NOTIFICATION_TYPE` و`NOTIFICATION_PRIORITY` و`ROUTES.NOTIFICATIONS`، وتسجيل slug القوائم مع بدائل عربية.
- مفاتيح `Notifications.RecipientNotFound` (عربي: «الإشعار غير موجود.») و`ActionUrlInvalid` وبقية رموز `Notifications.*` الواردة في `DomainErrors.cs` (لن تظهر عادةً للمستخدم لكنها تُغطَّى لقاعدة «لا نص إنجليزي»).
- `shared/lib/safeRelativeUrl.ts` مع قاعدته (`/x` نعم، `//x` و`https://x` و`javascript:` لا).
- **القبول:** `npm run build` و`npm run lint` ناجحان؛ لا استدعاءات جديدة.

### المرحلة 1 — شريحة `entities/notification` (القراءات)
- `model/types.ts` مطابق لـ API: `NotificationResponse` (`recipientId, notificationId, typeId, titleAr, bodyAr, priorityId, relatedDocumentId, relatedMovementId, actionUrl, isRead, readAt, createdAt`)، `NotificationListParams` (يشمل `search`, `sortBy`, `sortDescending`)، `UnreadCountResponse` (`count`, `byType`)، و`NotificationHubPayload` (حمولة الدفع). ثابت `NOTIFICATION_SORT_KEYS` في `shared/config` بقيم الخادم حرفيًا.
- `api/notificationApi.ts` (`list`, `unreadCount`) عبر `httpClient`.
- `model/queries.ts`: `notificationKeys` (تحمل `userId`)، `useNotifications(params)`، `useUnreadCount({ connected })`.
- `ui/` للأيقونة بحسب النوع، وشارة الأولوية، وعنصر صف مشترك بين القائمة المنسدلة والصفحة.
- README و`index.ts`.
- **القبول:** بناء ناجح؛ فحص يدوي لاستجابة حقيقية.

### المرحلة 2 — شريحة `features/notification-manage` (الكتابات)
- `useMarkRead(recipientId)` و`useMarkAllRead()` و`useDismiss(recipientId)` مع التحديث التفاؤلي والتراجع وإبطال `notificationKeys.all(userId)` عند `onSettled`.
- تأكيد قبل «قراءة الكل» لا يلزم (غير مدمّر)؛ أما «إخفاء» فبلا تأكيد أيضًا لأنه يُخفي من القائمة فقط (يُذكر في التلميح).
- **القبول:** العدّاد يتغيّر فورًا ويتطابق مع الخادم بعد الإبطال؛ الخطأ يظهر بالعربية عبر `toArabicErrorMessage(error)`.

### المرحلة 3 — الاتصال الحي `features/notification-live` + `NotificationLiveProvider`
- بناء/بدء/إيقاف الاتصال بنظافة تامة تحت `React.StrictMode` (لا اتصالان في التطوير: إيقاف الاتصال السابق بالانتظار قبل بدء الجديد، وتجاهل `start` الذي لُغي أثناء التفاوض).
- حالة الاتصال (`connected | reconnecting | disconnected`) في store صغير (zustand) يقرؤه الجرس.
- معالج `notificationReceived` (إزالة تكرار + تنبيه + إبطال)، و`onreconnected` (إبطال)، و`onclose` (سياسة التوكن المذكورة).
- إيقاف عند الخروج وإزالة ذاكرة الإشعارات.
- **القبول:** من حسابين: إحالة وثيقة من `admin` إلى قسم `user` ⇒ يظهر التنبيه للمستخدم المستلم خلال ~5 ثوانٍ (زمن الـ outbox) وتزيد العدّاد؛ المنفِّذ لا يتلقى شيئًا.

### المرحلة 4 — الجرس وصفحة الصندوق
- `widgets/notification-bell` (قائمة منسدلة بآخر 8 غير مقروءة/مقروءة، رأس فيه «قراءة الكل»، تذييل «عرض الكل»، مؤشر الاتصال، هيكل تحميل، حالة فارغة، وصول بلوحة المفاتيح).
- `widgets/notification-inbox` (الشبكة الكاملة بمرشحات النوع/غير المقروء، البحث والترتيب من الخادم عبر `useTableState`، الترقيم 10 افتراضيًا ومحدود 1..100، مُختار الأعمدة، قائمة سياقية) و`pages/notifications`.
- `AppShell` يقبل الخانة، و`AppRouter` يمرّر الجرس ويضيف المسار، و`shell-nav.ts` يضيف العنصر.
- **القبول:** المرشحات والصفحة في الـ URL وتبقى بعد التحديث؛ كل عمل متاح بلوحة المفاتيح؛ لا ملف يتجاوز ~250 سطرًا.

### المرحلة 5 — الوضع التعليمي
- `notifications.ts`: `notifications.bell`, `notifications.connection`, `notifications.grid`, `notifications.filters`, `notifications.read-all`, `notifications.dismiss` + `NOTIFICATIONS_GUIDE` (pageId) وتسجيله؛ `GuideRegion`/`GuideChip` بمعرّفات حرفية.
- **القبول:** `npm run lint` (بما فيه `check:guides`) و`npm run build` بلا أخطاء.

### المرحلة 6 — التحقق والوثائق
- تشغيل الـ SPA على API حقيقي (`npm run dev` + `.env`). سيناريوهات:
  1. إحالة وثيقة `admin` ⇒ `user`: تنبيه فوري + عدّاد + ظهور في الصفحة؛ فتح الوثيقة من التنبيه؛ قراءة، قراءة الكل، إخفاء.
  2. المنفِّذ لا يتلقى إشعاره؛ وثيقة سرّية لا تصل من لا يملك `VIEW`.
  3. إيقاف الخادم وإعادته: حالة «غير متصل» ثم إعادة الاتصال، وإبطال القائمة، وتسليم المتأخر **دون تكرار تنبيه**.
  4. انتهاء التوكن/تسجيل الخروج: إيقاف نظيف للاتصال، وعدم تسرّب إشعارات مستخدم سابق في نفس المتصفح.
  5. `404 Notifications.RecipientNotFound` لصف غير مملوك عبر استدعاء مباشر ⇒ رسالة عربية.
  6. تبويبان مفتوحان للحساب نفسه؛ فحص RTL والوضعين الفاتح والداكن والجوال ولوحة المفاتيح وقارئ الشاشة.
- `npm run build` و`npm run lint` و`dotnet build` / `dotnet test` (لا تغيير خلفي متوقع).
- الوثائق في نفس التغيير: README لكل مجلد لُمس (Contents/Status/Change Log)، `knowledge.md` (حذف وسم «Backend only: 10» وتفصيل الجرس والصفحة)، README الجذر وحالة الوحدات، `CLAUDE.md` و`AGENTS.md` إن تغيّر نص الحالة، وسطر «لاحقًا — خطة الواجهة» في خطة الخلفية يُحدَّث ليشير إلى هذه الخطة.

## 6. المخاطر والملاحظات

- **زمن التسليم ~5 ثوانٍ** (الـ outbox) لا لحظي تمامًا؛ لا نضيف معالجة خاصة، ونوثّق ذلك في المرحلة 6.
- **التوكن في سلسلة الاستعلام** مقتضى WebSocket لدى SignalR؛ القيد مذكور في الخطة الخلفية، ولا نُسجّل عنوان الاتصال في أي سجل في الواجهة.
- **StrictMode في التطوير** يركّب المزوّد مرتين؛ إدارة دورة حياة الاتصال هي أحد أكثر مواضع الأخطاء (سباق `stop` أثناء `start`) وتُغطّى بسيناريو يدوي في المرحلة 6.
- **حجم الحزمة:** تحميل SignalR كسولًا؛ يُتأكّد من `npm run build` أن المكتبة في chunk مستقل.
- **عدة تبويبات:** كل تبويب اتصال مستقل وتنبيه مستقل؛ قبول مقصود لا تنسيق بينها.
- **تحجيم أفقي للخادم:** مجموعة SignalR داخل عملية واحدة؛ قيد خلفي لا يخص الواجهة. يُوثَّق في `DEPLOYMENT.md` (أكثر من نسخة ⇒ backplane).
- **النشر والإنتاج:** CORS غير مضبوط في الخادم ويغطيه في التطوير وكيل Vite فقط. الإنتاج يحتاج reverse proxy يمرّر `/api` و`/hubs` (بما فيه ترقية WebSocket) أو ضبط CORS مع السماح بالاعتماد للـ hub. تُضاف هذه الملاحظة إلى `DEPLOYMENT.md` في المرحلة 6.
- **ترتيب التنفيذ:** المرحلة ب (الخلفية) تُرتكب وتُتحقَّق قبل بدء المراحل 0–6، لأن الواجهة تعتمد على العقد الجديد (`search`, `sortBy`, `byType`).

## 7. نقاط مفتوحة تحتاج قرارًا

1. ~~البحث والترتيب~~ **حُسم:** يُنفَّذان في الخادم (المرحلة ب1).
2. **أنواع إشعار إضافية (اختيارية، المقترح: خارج هذه الخطة):** منح صلاحية وصول لوثيقة، إضافة/إزالة رابط (الوحدة 08)، رفع مرفق أو نسخة جديدة، فك الأرشفة. أحداثها موجودة، لكنها تتطلب أنواعًا جديدة (12 وما بعده) في البذر مع ترحيل إضافي، واستدعاءات ناشر في معالجات تلك الوحدات، وقواعد مستلمين (من يُبلَّغ؟ وماذا عن السرّية؟)، واختبارات، ثم تحديث `NOTIFICATION_TYPE` والأرقام في الواجهة. إن وافقت تُفرد لها خطة صغيرة منفصلة بعد هذه.
3. **بطاقة على لوحة التحكم** بعدد غير المقروء: المقترح خارج هذه الخطة (الجرس يكفي).
4. **مدد الاحتفاظ (90 و30 يومًا):** نقطة مفتوحة في الخطة الخلفية؛ تأكد من اعتمادها لأنها حذف دائم.

## 8. خارج النطاق

- قنوات EMAIL/SMS وتفضيلات المستخدم (كتم أنواع، ساعات هدوء) — خارج النطاق في الخلفية أيضًا.
- صوت التنبيه وإشعارات المتصفح (Notification API) والدفع في الخلفية (Service Worker).
- «تعليم كغير مقروء» (غير مدعوم في API).
- تنسيق التبويبات المتعددة (BroadcastChannel) وbackplane الخادم.
- أنواع الإشعار الإضافية (القسم 7، البند 2) — خطة منفصلة إن اعتُمدت.
- اختبارات آلية للواجهة (لا مشغّل اختبارات؛ البوابات هي `build` و`lint` والتشغيل الفعلي).

---

## 9. ما اختلف عن الخطة (بعد التنفيذ، 2026-11-04)

**الحالة:** المرحلة ب والمراحل 0–5 منفّذة. `dotnet test` 743 ناجحًا (740 + 3)، و`npm run build` و`npm run lint` ناجحان.

- **الترتيب الافتراضي والاتجاه:** `sortDescending` الافتراضي في الخادم `false` كبقية القوائم (لا `true` كما في المسودة)؛ وبلا `sortBy` يبقى الترتيب «الأحدث أولًا» دائمًا. الواجهة ترسل الاتجاه صراحةً. مفاتيح الـ URL هي مفاتيح `useTableState` المعتادة: `search` و`sort` و`dir` و`page` و`pageSize`، ومرشحاها `unread=1` و`type`. قيمة `sortBy` غير معروفة تعود للافتراضي بصمت.
- **تهريب البحث:** `LIKE` بحرف هروب `\` يعالج `%` و`_` و`[`؛ وجرّبت ذلك باختبار (البحث عن `%` يجد النص الحرفي فقط).
- **`byType`:** يُحسب فقط حين يكون المجموع أكبر من صفر (استعلام واحد إضافي).
- **الجرس بلا دليل تعليمي:** يقع في الشريط العلوي خارج أي `GuideProvider`، فحُذف إدخال `notifications.bell` من الكتالوج؛ بقيت أدلة الصفحة (`grid`, `filters`, `read-all`).
- **مكان الاتصال الحي:** `NotificationLiveLayout` في `app/router` يلفّ مسارات القشرة بدل مزوّد في `app/providers`، لأن `useNavigate` يحتاج الموجّه (`BrowserRouter` داخل `AppRouter`). وهو أيضًا من يربط `notification-live` بـ`notification-manage` (ميزتان لا تستورد إحداهما الأخرى).
- **الإضافات:** `ENV.HUB_BASE_URL` (افتراضيًا `/hubs`)، و`safeRelativeUrl` في `shared/lib`.

**ما جُرّب على API حي:** ظهور الإشعار في الصفحة، دفع لحظي (تنبيه + عدّاد الجرس + تحديث الجدول بلا إعادة تحميل)، قراءة من الجرس وفتح الوثيقة من إشعار، تطابق عدّاد الجرس مع `unread-count` في الخادم، البحث والترتيب من الـ URL، وانقطاع الخادم ثم عودته (الجرس يعرض «غير متصل» ثم «التحديث الفوري يعمل» تلقائيًا). التشغيل تم تحت `StrictMode` في وضع التطوير دون اتصال مكرر ظاهر.

**لم يُتحقق منه بعد (المرحلة 6 اليدوية):** تنبيه عاجل يبقى حتى الإغلاق، انتهاء التوكن أثناء الاتصال، تبديل مستخدم في المتصفح نفسه (مفاتيح الذاكرة محمّلة بـ`userId` لكن لم تُجرَّب)، وثيقة سرّية، تبويبان لنفس الحساب، فحص بصري للوضع الداكن والجوال ولوحة المفاتيح وقارئ الشاشة، والتأكد من أن `@microsoft/signalr` في chunk مستقل. كما لم تُضَف ملاحظة CORS/backplane إلى `DEPLOYMENT.md` بعد.
