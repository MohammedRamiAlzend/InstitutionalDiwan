import { useRef, useState } from 'react'
import { ImagePlus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { BrandMark, useBranding } from '@/entities/branding'
import { toArabicErrorMessage } from '@/shared/api'
import { Button } from '@/shared/ui'
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
      setPickError('الملف ليس صورة مقبولة. اختر PNG أو JPEG أو WebP أو SVG.')
      return
    }
    if (file.size > MAX_BYTES) {
      setPickError('حجم الشعار يتجاوز الحد الأقصى (1 ميغابايت).')
      return
    }

    try {
      await upload.mutateAsync(file)
      toast.success('تم تحديث شعار المؤسسة.')
    } catch {
      // The mutation error is shown below in Arabic.
    }
  }

  const onRemove = async () => {
    setPickError(null)
    upload.reset()
    try {
      await remove.mutateAsync()
      toast.success('تمت إزالة شعار المؤسسة.')
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
            {hasLogo ? 'استبدال الشعار' : 'رفع شعار'}
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
              إزالة الشعار
            </Button>
          )}
        </div>
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED_TYPES.join(',')}
          className="hidden"
          aria-label="ملف شعار المؤسسة"
          onChange={(event) => {
            const file = event.target.files?.[0]
            event.target.value = ''
            void onPick(file)
          }}
        />
      </div>
      <p className="text-xs text-muted">
        PNG أو JPEG أو WebP أو SVG، حتى 1 ميغابايت. يظهر الشعار في صفحة الدخول وفي القائمة الجانبية وفي
        أيقونة التبويب، وتُستحسن الصورة المربعة بخلفية شفافة.
      </p>
      {error && (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  )
}
