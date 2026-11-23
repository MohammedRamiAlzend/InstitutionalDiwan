import { useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Crop, FlipHorizontal, FlipVertical, Loader2, RotateCcw, RotateCw, Save, Undo2 } from 'lucide-react'
import { toast } from 'sonner'
import { driveItemApi, useDriveEffectiveRole, type DriveItemResponse } from '@/entities/drive-item'
import { useUploadDriveFileMutation } from '@/features/drive-upload'
import { toArabicErrorMessage } from '@/shared/api'
import { DRIVE_ROLE } from '@/shared/config'
import { t } from '@/shared/lib'
import { Button, Label } from '@/shared/ui'
import {
  blobToDataUrl,
  cropImageDataUrl,
  editableImageMimeType,
  encodeImage,
  transformImageDataUrl,
  type ImageTransform,
} from '../model/imageEditing'

interface ImageEditorProps {
  item: DriveItemResponse
  open: boolean
}

interface FractionRect {
  x: number
  y: number
  width: number
  height: number
}

function clampUnit(value: number): number {
  return Math.min(1, Math.max(0, value))
}

/**
 * In-app editor for `.png`/`.jpg`/`.jpeg` Drive images: rotate, flip and crop on a canvas, then save
 * the result as a new immutable version through the existing presigned upload flow
 * (initiate → PUT → complete), so previous versions stay intact and the space usage is updated.
 * Tools are hidden for Viewers, who still see the image itself. Every intermediate edit stays local
 * until «حفظ إصدار جديد» is pressed; «إعادة التعيين» returns to the stored bytes.
 */
export function ImageEditor({ item, open }: ImageEditorProps) {
  const imageQuery = useQuery({
    queryKey: ['drive-image', item.itemId],
    queryFn: async () => {
      const { blob } = await driveItemApi.content(item.itemId)
      return blobToDataUrl(blob)
    },
    enabled: open,
    staleTime: 0,
    gcTime: 0,
  })
  const roleQuery = useDriveEffectiveRole(item.itemId, { enabled: open })
  const uploadMutation = useUploadDriveFileMutation()
  const queryClient = useQueryClient()
  const canEdit = (roleQuery.data?.roleId ?? DRIVE_ROLE.None) >= DRIVE_ROLE.Editor

  // The working image is keyed by item, so switching files discards the local edits during render.
  const [working, setWorking] = useState<{ itemId: string; url: string } | null>(null)
  const [isTransforming, setIsTransforming] = useState(false)
  const [isCropping, setIsCropping] = useState(false)
  const [selection, setSelection] = useState<FractionRect | null>(null)
  const [dragStart, setDragStart] = useState<{ x: number; y: number } | null>(null)
  const imageRef = useRef<HTMLImageElement>(null)

  const currentUrl = working?.itemId === item.itemId ? working.url : (imageQuery.data ?? null)
  const isDirty = working?.itemId === item.itemId
  const busy = isTransforming || uploadMutation.isPending
  const mimeType = editableImageMimeType(item.name)

  const applyTransform = async (transform: ImageTransform) => {
    if (!currentUrl || busy) return
    setIsTransforming(true)
    try {
      const url = await transformImageDataUrl(currentUrl, transform)
      setWorking({ itemId: item.itemId, url })
      setSelection(null)
    } catch {
      toast.error(t('drive.image.encode-failed'))
    } finally {
      setIsTransforming(false)
    }
  }

  const pointOf = (event: ReactPointerEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect()
    return {
      x: clampUnit((event.clientX - rect.left) / rect.width),
      y: clampUnit((event.clientY - rect.top) / rect.height),
    }
  }

  const handlePointerDown = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (!canEdit || !isCropping || busy) return
    const point = pointOf(event)
    event.currentTarget.setPointerCapture(event.pointerId)
    setDragStart(point)
    setSelection({ x: point.x, y: point.y, width: 0, height: 0 })
  }

  const handlePointerMove = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (!dragStart) return
    const point = pointOf(event)
    setSelection({
      x: Math.min(dragStart.x, point.x),
      y: Math.min(dragStart.y, point.y),
      width: Math.abs(point.x - dragStart.x),
      height: Math.abs(point.y - dragStart.y),
    })
  }

  const handlePointerUp = () => setDragStart(null)

  const handleApplyCrop = async () => {
    const image = imageRef.current
    if (!currentUrl || !image || !selection || busy) return
    const width = Math.round(selection.width * image.naturalWidth)
    const height = Math.round(selection.height * image.naturalHeight)
    if (width < 2 || height < 2) return
    setIsTransforming(true)
    try {
      const url = await cropImageDataUrl(currentUrl, {
        x: selection.x * image.naturalWidth,
        y: selection.y * image.naturalHeight,
        width,
        height,
      })
      setWorking({ itemId: item.itemId, url })
      setSelection(null)
      setIsCropping(false)
    } catch {
      toast.error(t('drive.image.encode-failed'))
    } finally {
      setIsTransforming(false)
    }
  }

  const handleReset = () => {
    setWorking(null)
    setSelection(null)
    setIsCropping(false)
  }

  const handleSave = async () => {
    if (!currentUrl || !isDirty || !mimeType || busy) return
    try {
      const blob = await encodeImage(currentUrl, mimeType)
      if (!blob) {
        toast.error(t('drive.image.encode-failed'))
        return
      }
      const file = new File([blob], item.name, { type: mimeType })
      await uploadMutation.mutateAsync({
        target: {
          spaceId: item.spaceId,
          parentId: item.parentId,
          fileItemId: item.itemId,
          fileName: item.name,
        },
        file,
      })
      setWorking(null)
      setSelection(null)
      setIsCropping(false)
      void queryClient.invalidateQueries({ queryKey: ['drive-image', item.itemId] })
      toast.success(t('drive.image.saved'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  if (imageQuery.isLoading) {
    return (
      <p className="flex h-full min-h-[60vh] items-center justify-center gap-2 text-sm text-muted">
        <Loader2 className="size-5 animate-spin" aria-hidden />
        <Label k="drive.image.loading" />
      </p>
    )
  }
  if (imageQuery.error || !currentUrl) {
    return (
      <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger">
        {imageQuery.error ? toArabicErrorMessage(imageQuery.error) : t('drive.image.encode-failed')}
      </p>
    )
  }

  if (!canEdit) {
    return (
      <div className="flex h-full min-h-0 flex-col items-center justify-center gap-2">
        <img src={currentUrl} alt={item.name} className="max-h-full max-w-full object-contain" />
        <p className="text-xs text-muted">
          <Label k="drive.editor.view-only" />
        </p>
      </div>
    )
  }

  return (
    <div className="flex h-full min-h-[60vh] w-full flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void applyTransform({ rotate: 270 })}
          disabled={busy}
          title={t('drive.image.rotate-left')}
          aria-label={t('drive.image.rotate-left')}
        >
          <RotateCcw className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void applyTransform({ rotate: 90 })}
          disabled={busy}
          title={t('drive.image.rotate-right')}
          aria-label={t('drive.image.rotate-right')}
        >
          <RotateCw className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void applyTransform({ flipHorizontal: true })}
          disabled={busy}
          title={t('drive.image.flip-horizontal')}
          aria-label={t('drive.image.flip-horizontal')}
        >
          <FlipHorizontal className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void applyTransform({ flipVertical: true })}
          disabled={busy}
          title={t('drive.image.flip-vertical')}
          aria-label={t('drive.image.flip-vertical')}
        >
          <FlipVertical className="size-4" aria-hidden />
        </Button>

        <span className="mx-1 h-6 w-px bg-border" aria-hidden />

        <Button
          type="button"
          size="sm"
          variant={isCropping ? 'primary' : 'outline'}
          onClick={() => {
            setIsCropping((current) => !current)
            setSelection(null)
          }}
          disabled={busy}
        >
          <Crop className="size-4" aria-hidden />
          <Label k="drive.image.crop" />
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void handleApplyCrop()}
          disabled={busy || !isCropping || !selection || selection.width <= 0 || selection.height <= 0}
        >
          <Label k="drive.image.crop-apply" />
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={handleReset}
          disabled={busy || !isDirty}
        >
          <Undo2 className="size-4" aria-hidden />
          <Label k="drive.image.reset" />
        </Button>

        <span className="flex-1" />

        <Button type="button" size="sm" onClick={() => void handleSave()} disabled={busy || !isDirty} loading={uploadMutation.isPending}>
          <Save className="size-4" aria-hidden />
          <Label k="drive.image.save" />
        </Button>
      </div>

      <p className="text-xs text-muted">
        {isCropping ? <Label k="drive.image.crop-hint" /> : <Label k="drive.image.hint" />}
      </p>

      <div className="relative flex min-h-0 flex-1 items-center justify-center overflow-hidden rounded-lg border border-border bg-surface p-2">
        <div
          className={`relative inline-block touch-none select-none ${isCropping ? 'cursor-crosshair' : ''}`}
          onPointerDown={handlePointerDown}
          onPointerMove={handlePointerMove}
          onPointerUp={handlePointerUp}
          onPointerCancel={handlePointerUp}
        >
          <img
            ref={imageRef}
            src={currentUrl}
            alt={item.name}
            draggable={false}
            className="block max-h-[62vh] max-w-full object-contain"
          />
          {selection && selection.width > 0 && selection.height > 0 && (
            <div
              className="pointer-events-none absolute border-2 border-focus-ring bg-focus-ring/20"
              style={{
                left: `${selection.x * 100}%`,
                top: `${selection.y * 100}%`,
                width: `${selection.width * 100}%`,
                height: `${selection.height * 100}%`,
              }}
              aria-hidden
            />
          )}
        </div>
        {isTransforming && (
          <div className="absolute inset-0 flex items-center justify-center bg-surface/60">
            <Loader2 className="size-6 animate-spin text-muted" aria-hidden />
          </div>
        )}
      </div>
    </div>
  )
}
