import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { NotificationInbox } from '@/widgets/notification-inbox'

/**
 * `/notifications` — the caller's own inbox. Needs no permission code: every route behind it is
 * scoped to the signed-in user by the server.
 */
export function NotificationsPage() {
  return (
    <GuideProvider pageId="notifications">
      <div className="flex flex-col gap-6">
        <PageHeader
          title="الإشعارات"
          description="تنبيهات الوثائق والإحالات الموجّهة إليك أو إلى قسمك. تصل لحظيًا عند اتصال التحديث الفوري، وإلا فتُحدَّث القائمة عند فتح الصفحة."
          pageGuide="notifications"
        />
        <GuideRegion id="notifications.grid">
          <NotificationInbox />
        </GuideRegion>
      </div>
    </GuideProvider>
  )
}
