import { useRef, useState } from 'react'
import { ImagePlus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { BrandMark, useBranding } from '@/entities/branding'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Label } from '@/shared/ui'
import {
  useRemoveInstitutionLogoMutation,
  useUploadInstitutionLogoMutation,
} from '../api/institutionLogoMutations'

/** Mirrors the server: PNG, JPEG, WebP or plain SVG, at most 1 MB. */
const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/webp', 'image/svg+xml']
const MAX_BYTES = 1024 * 1024

/**
 * Uploads or removes the institution logo. A change applies at once (it is not part of the settings
 * form's save) and the sign-in page, the shell and the browser tab pick it up.
 */
export function InstitutionLogoField() {
  const inputRef = useRef<HTMLInputElement>(null)
  const [pickError, setPickError] = useState<string | null>(null)
  const hasLogo = Boolean(useBranding().data?.logoUpdatedAt)

  const upload = useUploadInstitutionLogoMutation()
  const remove = useRemoveInstitutionLogoMutation()
  const busy = upload.isPending || remove.isPending
  const error = pickError ?? (upload.error || remove.error ? toArabicErrorMessage(upload.error ?? remove.error) : null)

  const onPick = async (file: File | undefined) => {
    setPickError(null)
    upload.reset()
    remove.reset()
    if (!file) return

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setPickError(t('institution.logo.invalid-type'))
      return
    }
    if (file.size > MAX_BYTES) {
      setPickError(t('institution.logo.too-large'))
      return
    }

    try {
      await upload.mutateAsync(file)
      toast.success(t('institution.logo.toast-uploaded'))
    } catch {
      // The mutation error is shown below in Arabic.
    }
  }

  const onRemove = async () => {
    setPickError(null)
    upload.reset()
    try {
      await remove.mutateAsync()
      toast.success(t('institution.logo.toast-removed'))
    } catch {
      // The mutation error is shown below in Arabic.
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-4">
        <BrandMark className="size-16 border border-border" />
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            loading={upload.isPending}
            disabled={busy}
            onClick={() => inputRef.current?.click()}
          >
            <ImagePlus className="size-4" aria-hidden />
            {hasLogo ? <Label k="institution.logo.replace" /> : <Label k="institution.logo.upload" />}
          </Button>
          {hasLogo && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              loading={remove.isPending}
              disabled={busy}
              onClick={() => void onRemove()}
            >
              <Trash2 className="size-4" aria-hidden />
              <Label k="institution.logo.remove" />
            </Button>
          )}
        </div>
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED_TYPES.join(',')}
          className="hidden"
          aria-label={t('institution.logo.input-aria')}
          onChange={(event) => {
            const file = event.target.files?.[0]
            event.target.value = ''
            void onPick(file)
          }}
        />
      </div>
      <p className="text-xs text-muted">
        {t('institution.logo.hint')}
      </p>
      {error && (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  )
}
