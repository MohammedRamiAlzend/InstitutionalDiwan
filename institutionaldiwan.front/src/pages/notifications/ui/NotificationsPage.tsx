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
          titleKey="page.notifications.title"
          descriptionKey="page.notifications.description"
          pageGuide="notifications"
        />
        <GuideRegion id="notifications.grid">
          <NotificationInbox />
        </GuideRegion>
      </div>
    </GuideProvider>
  )
}
