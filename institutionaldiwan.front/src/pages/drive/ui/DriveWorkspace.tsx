import { useEffect, useRef, useState } from 'react'
import { Settings } from 'lucide-react'
import { toast } from 'sonner'
import type { DriveItemResponse } from '@/entities/drive-item'
import { useDriveSpace, useDriveSpaces } from '@/entities/drive-space'
import { useEnsurePersonalSpaceMutation } from '@/features/drive-space-manage'
import { RequestDeletionDialog, useDirectDeleteDriveItemMutation } from '@/features/drive-deletion'
import { DriveSettingsDialog } from '@/features/drive-space-manage'
import { useSessionStore } from '@/entities/user'
import { Permissions } from '@/shared/config'
import {
  UploadDriveDialog,
  downloadDriveFile,
  type DriveUploadTarget,
} from '@/features/drive-upload'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, ConfirmDialog, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { t } from '@/shared/lib'
import { DriveSidebar } from '@/widgets/drive-sidebar'
import { DriveBrowser } from '@/widgets/drive-browser'
import { DrivePreviewDialog } from '@/widgets/drive-preview-dialog'
import { DriveVersionsPanel } from '@/widgets/drive-versions-panel'
import { DrivePermissionsPanel } from '@/widgets/drive-permissions-panel'
import { DriveDeletionInbox } from '@/widgets/drive-deletion-inbox'
import { DriveTrashPanel } from '@/widgets/drive-trash-panel'
import { useDriveNavigation } from '../model/useDriveNavigation'

/**
 * The Drive workspace body. Owns the URL navigation state and the spaces overview, ensures the
 * personal space exists on first entry, and composes the sidebar rail with the content region.
 * Browse modes mount `DriveBrowser` with upload/preview/download/editor actions; trash and
 * deletion-requests are separate URL-driven views.
 */
export function DriveWorkspace() {
  const nav = useDriveNavigation()
  const { data: overview, isLoading, error, refetch } = useDriveSpaces()
  const { mutate: ensurePersonal, isPending: isEnsuring } = useEnsurePersonalSpaceMutation()
  const canManage = useSessionStore((state) => state.user?.permissions.includes(Permissions.DriveManage) ?? false)
  const directDelete = useDirectDeleteDriveItemMutation()
  const ensuredRef = useRef(false)

  const [uploadTarget, setUploadTarget] = useState<DriveUploadTarget | null>(null)
  const [uploadFiles, setUploadFiles] = useState<File[]>([])
  const [previewTarget, setPreviewTarget] = useState<DriveItemResponse | null>(null)
  const [versionsTarget, setVersionsTarget] = useState<DriveItemResponse | null>(null)
  const [permissionsTarget, setPermissionsTarget] = useState<DriveItemResponse | null>(null)
  const [deletionTarget, setDeletionTarget] = useState<DriveItemResponse | null>(null)
  const [directDeleteTarget, setDirectDeleteTarget] = useState<DriveItemResponse | null>(null)
  const [settingsOpen, setSettingsOpen] = useState(false)

  const personal = overview?.personal ?? null
  const shared = overview?.shared ?? []

  // «ملفاتي» is created lazily the first time the caller opens Drive without a personal space.
  useEffect(() => {
    if (
      !isLoading &&
      overview &&
      overview.personal === null &&
      !ensuredRef.current &&
      !isEnsuring
    ) {
      ensuredRef.current = true
      ensurePersonal()
    }
  }, [isLoading, overview, isEnsuring, ensurePersonal])

  const isBrowseView = nav.view === 'my-files' || nav.view === 'shared' || nav.view === 'starred'

  // Uploads land in the open folder of the active space; the cross-space starred view has no target.
  const folderTarget: DriveUploadTarget | null =
    nav.view === 'my-files' && personal
      ? { spaceId: personal.spaceId, parentId: nav.folderId }
      : nav.view === 'shared' && nav.spaceId
        ? { spaceId: nav.spaceId, parentId: nav.folderId }
        : null

  const uploadSpaceQuery = useDriveSpace(uploadTarget?.spaceId, {
    enabled: Boolean(uploadTarget?.spaceId),
  })
  const remainingQuotaBytes = uploadSpaceQuery.data
    ? uploadSpaceQuery.data.effectiveQuotaBytes - uploadSpaceQuery.data.usedBytes
    : null

  const openUpload = () => {
    if (!folderTarget) return
    setUploadFiles([])
    setUploadTarget(folderTarget)
  }

  const handleDropFiles = (files: File[]) => {
    if (!folderTarget) return
    setUploadFiles(files)
    setUploadTarget(folderTarget)
  }

  const handleUploadVersion = (item: DriveItemResponse) => {
    setUploadFiles([])
    setUploadTarget({ spaceId: item.spaceId, fileItemId: item.itemId, fileName: item.name })
  }

  const handleDownload = (item: DriveItemResponse) => {
    void downloadDriveFile(item).catch((err) => toast.error(toArabicErrorMessage(err)))
  }

  const closeUpload = (nextOpen: boolean) => {
    if (!nextOpen) {
      setUploadTarget(null)
      setUploadFiles([])
    }
  }

  return (
    <div className="flex flex-col gap-6 lg:gap-7">
      <PageHeader
        titleKey="page.drive.title"
        descriptionKey="page.drive.description"
        pageGuide="drive"
        actions={canManage ? <Button variant="outline" onClick={() => setSettingsOpen(true)}><Settings className="size-4" aria-hidden /><Label k="drive.dialog.settings-title" /></Button> : undefined}
      />
      {error ? (
        <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-border bg-surface p-8 text-center">
          <p className="text-sm text-danger">{toArabicErrorMessage(error)}</p>
          <Button variant="outline" onClick={() => refetch()}>
            <Label k="drive.retry" />
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-5 lg:grid lg:grid-cols-[17rem_minmax(0,1fr)] lg:items-start lg:gap-6">
          <GuideRegion id="drive.sidebar" className="lg:sticky lg:top-4">
            <DriveSidebar
              personal={personal}
              shared={shared}
              isLoading={isLoading}
              view={nav.view}
              spaceId={nav.spaceId}
              onSelectView={nav.setView}
              onSelectSpace={nav.setSpace}
            />
          </GuideRegion>
          <GuideRegion id="drive.browser" className="min-w-0 flex-1">
            {isBrowseView ? (
              <DriveBrowser
                view={nav.view}
                spaceId={nav.spaceId}
                folderId={nav.folderId}
                personal={personal}
                shared={shared}
                onNavigateFolder={nav.setFolder}
                onUpload={folderTarget ? openUpload : undefined}
                onDropFiles={folderTarget ? handleDropFiles : undefined}
                actions={{
                  onOpenFile: setPreviewTarget,
                  onDownload: handleDownload,
                  onUploadVersion: handleUploadVersion,
                  onVersions: setVersionsTarget,
                  onPermissions: setPermissionsTarget,
                  onRequestDeletion: setDeletionTarget,
                  onDirectDelete: canManage ? setDirectDeleteTarget : undefined,
                }}
              />
            ) : nav.view === 'trash' ? (
            <DriveTrashPanel />
            ) : (
            <DriveDeletionInbox />
            )}
          </GuideRegion>
        </div>
      )}

      {uploadTarget && (
        <GuideRegion id="drive.upload">
          <UploadDriveDialog
            target={uploadTarget}
            remainingQuotaBytes={remainingQuotaBytes}
            initialFiles={uploadFiles}
            open
            onOpenChange={closeUpload}
          />
        </GuideRegion>
      )}
      {previewTarget && (
        <DrivePreviewDialog
          item={previewTarget}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setPreviewTarget(null)
          }}
        />
      )}
      {versionsTarget && (
        <GuideRegion id="drive.versions">
          <DriveVersionsPanel
            item={versionsTarget}
            open
            onOpenChange={(nextOpen) => {
              if (!nextOpen) setVersionsTarget(null)
            }}
          />
        </GuideRegion>
      )}
      {permissionsTarget && (
        <GuideRegion id="drive.permissions">
          <DrivePermissionsPanel
            item={permissionsTarget}
            open
            onOpenChange={(nextOpen) => {
              if (!nextOpen) setPermissionsTarget(null)
            }}
          />
        </GuideRegion>
      )}
      {deletionTarget && (
        <RequestDeletionDialog
          itemId={deletionTarget.itemId}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setDeletionTarget(null)
          }}
        />
      )}
      <ConfirmDialog
        open={Boolean(directDeleteTarget)}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) setDirectDeleteTarget(null)
        }}
        title={t('drive.confirm.move-to-trash-title')}
        description={t('drive.confirm.move-to-trash-description')}
        confirmLabel={t('drive.action.move-to-trash')}
        onConfirm={() => {
          if (!directDeleteTarget) return
          void directDelete
            .mutateAsync(directDeleteTarget.itemId)
            .then(() => {
              toast.success(t('drive.toast.deleted'))
              setDirectDeleteTarget(null)
            })
            .catch((error) => toast.error(toArabicErrorMessage(error)))
        }}
        loading={directDelete.isPending}
        destructive
      />
      <DriveSettingsDialog open={settingsOpen} onOpenChange={setSettingsOpen} />
    </div>
  )
}
