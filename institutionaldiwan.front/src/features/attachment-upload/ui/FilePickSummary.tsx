import { Loader2, Paperclip } from 'lucide-react'
import { formatFileSize, formatShortId } from '@/shared/lib'
import type { LocalChecksum } from '../model/useLocalChecksum'

interface FilePickSummaryProps {
  file: File
  checksum: LocalChecksum
}

/**
 * The picked file with its size and locally computed digest. Sharing one view keeps the two upload
 * dialogs honest about the same thing: whether a comparison with the stored checksum is even possible.
 */
export function FilePickSummary({ file, checksum }: FilePickSummaryProps) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-border bg-surface px-3 py-2">
      <p className="flex items-center gap-1.5 text-xs text-foreground">
        <Paperclip className="size-3.5 shrink-0" aria-hidden />
        <span dir="ltr" className="truncate" title={file.name}>
          {file.name}
        </span>
        <span className="text-muted">· {formatFileSize(file.size)}</span>
      </p>
      {checksum.isHashing ? (
        <p className="flex items-center gap-1.5 text-xs text-muted">
          <Loader2 className="size-3.5 animate-spin" aria-hidden />
          جارٍ حساب البصمة…
        </p>
      ) : checksum.error ? (
        <p className="text-xs text-danger">{checksum.error}</p>
      ) : checksum.digest ? (
        <p className="text-xs text-muted">
          البصمة المحلية:{' '}
          <code dir="ltr" className="text-foreground" title={checksum.digest}>
            {formatShortId(checksum.digest)}
          </code>
        </p>
      ) : null}
    </div>
  )
}
