import { useEffect, useRef, useState, type ReactNode } from 'react'
import { ClipboardList, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import type { DriveItemResponse } from '@/entities/drive-item'
import { useDriveSpace, useDriveSpaces } from '@/entities/drive-space'
import { useEnsurePersonalSpaceMutation } from '@/features/drive-space-manage'
import {
  UploadDriveDialog,
  downloadDriveFile,
  type DriveUploadTarget,
} from '@/features/drive-upload'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { DriveSidebar } from '@/widgets/drive-sidebar'
import { DriveBrowser } from '@/widgets/drive-browser'
import { DrivePreviewDialog } from '@/widgets/drive-preview-dialog'
import { useDriveNavigation } from '../model/useDriveNavigation'

/**
 * The Drive workspace body. Owns the URL navigation state and the spaces overview, ensures the
 * personal space exists on first entry, and composes the sidebar rail with the content region.
 * Browse modes mount `DriveBrowser` with the upload/preview/download actions wired; trash and
 * deletion-requests land in Phase 5.
 */
export function DriveWorkspace() {
  const nav = useDriveNavigation()
  const { data: overview, isLoading, error, refetch } = useDriveSpaces()
  const { mutate: ensurePersonal, isPending: isEnsuring } = useEnsurePersonalSpaceMutation()
  const ensuredRef = useRef(false)

  const [uploadTarget, setUploadTarget] = useState<DriveUploadTarget | null>(null)
  const [uploadFiles, setUploadFiles] = useState<File[]>([])
  const [previewTarget, setPreviewTarget] = useState<DriveItemResponse | null>(null)

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
    <div className="flex flex-col gap-6">
      <PageHeader
        titleKey="page.drive.title"
        descriptionKey="page.drive.description"
        pageGuide="drive"
      />
      {error ? (
        <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-border bg-surface p-8 text-center">
          <p className="text-sm text-danger">{toArabicErrorMessage(error)}</p>
          <Button variant="outline" onClick={() => refetch()}>
            <Label k="drive.retry" />
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start">
          <GuideRegion id="drive.sidebar" className="lg:w-72 lg:shrink-0">
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
                }}
              />
            ) : nav.view === 'trash' ? (
              <DriveViewPlaceholder icon={<Trash2 className="size-10 text-muted" aria-hidden />} labelKey="drive.trash" />
            ) : (
              <DriveViewPlaceholder icon={<ClipboardList className="size-10 text-muted" aria-hidden />} labelKey="drive.view.deletion-requests" />
            )}
          </GuideRegion>
        </div>
      )}

      {uploadTarget && (
        <UploadDriveDialog
          target={uploadTarget}
          remainingQuotaBytes={remainingQuotaBytes}
          initialFiles={uploadFiles}
          open
          onOpenChange={closeUpload}
        />
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
    </div>
  )
}

function DriveViewPlaceholder({ icon, labelKey }: { icon: ReactNode; labelKey: 'drive.trash' | 'drive.view.deletion-requests' }) {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border bg-surface p-8 text-center">
      {icon}
      <p className="text-sm font-medium text-foreground">
        <Label k={labelKey} />
      </p>
    </div>
  )
}