import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider } from '@/shared/ui'
import { DriveWorkspace } from './DriveWorkspace'

/**
 * `/drive` — Module 11 «الملفات»: a personal space plus shared spaces, folders and versioned files,
 * hierarchical permissions, approval-gated deletion and a temporary trash. Needs `DRIVE_USE`.
 */
export function DrivePage() {
  return (
    <RequirePermission permission={Permissions.DriveUse}>
      <GuideProvider pageId="drive">
        <DriveWorkspace />
      </GuideProvider>
    </RequirePermission>
  )
}