import { useState } from 'react'
import { Loader2, RefreshCw } from 'lucide-react'
import {
  ChecksumBadge,
  currentVersionOf,
  useVerifyChecksum,
  type DocumentFileResponse,
} from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { formatShortId } from '@/shared/lib'
import { Button, Dialog, Input } from '@/shared/ui'
import { useLocalChecksum } from '../model/useLocalChecksum'

interface VerifyChecksumDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

function DigestLine({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <p className="flex flex-wrap items-baseline gap-1.5 text-xs">
      <span className="text-muted">{label}:</span>
      <code dir="ltr" className="break-all text-foreground" title={value}>
        {formatShortId(value)}
      </code>
      {hint && <span className="text-muted">{hint}</span>}
    </p>
  )
}

function ComparisonLine({ label, isMatch }: { label: string; isMatch: boolean | null }) {
  return (
    <p className="flex flex-wrap items-center gap-2 text-xs">
      <span className="text-muted">{label}</span>
      {isMatch === null ? (
        <span className="text-muted">غير متاحة — لم تُحسب بصمة محلية.</span>
      ) : (
        <ChecksumBadge isMatch={isMatch} />
      )}
    </p>
  )
}

/**
 * Verifies the current version's integrity: the server re-reads the stored bytes and recomputes
 * SHA-256, and the dialog lines that up against the checksum recorded at upload time. Picking the
 * same file from disk adds a third digest, so a transfer error can be told apart from a storage one.
 */
export function VerifyChecksumDialog({
  file,
  open,
  onOpenChange,
}: VerifyChecksumDialogProps) {
  const version = currentVersionOf(file)
  const verifyQuery = useVerifyChecksum({ fileId: file.fileId }, { enabled: open })
  const checksum = useLocalChecksum()
  const [pickedName, setPickedName] = useState<string | null>(null)

  const result = verifyQuery.data
  const recorded = result?.recordedChecksumSha256 ?? version?.checksumSha256 ?? ''
  const computed = result?.computedChecksumSha256 ?? ''
  const local = checksum.digest

  const lower = (value: string) => value.toLowerCase()
  const matchesLocal = (value: string) => (local ? lower(value) === lower(local) : null)

  const pickFile = async (next: File | null) => {
    setPickedName(next?.name ?? null)
    await checksum.compute(next)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="التحقق من بصمة الملف"
      description={`يعيد الخادم حساب SHA-256 للإصدار ${result?.versionNumber ?? version?.versionNumber ?? 'الحالي'} من «${file.title}» ويقارنه بالبصمة المسجّلة وقت الرفع.`}
      size="md"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => void verifyQuery.refetch()}
            loading={verifyQuery.isFetching}
          >
            <RefreshCw className="size-4" aria-hidden />
            إعادة التحقق
          </Button>
          <Button type="button" onClick={() => onOpenChange(false)}>
            إغلاق
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3">
        {verifyQuery.isPending ? (
          <p className="flex items-center gap-2 text-sm text-muted">
            <Loader2 className="size-4 animate-spin" aria-hidden />
            جارٍ إعادة حساب البصمة من القرص…
          </p>
        ) : verifyQuery.error ? (
          <p role="alert" className="text-sm text-danger">
            {toArabicErrorMessage(verifyQuery.error)}
          </p>
        ) : result ? (
          <>
            <DigestLine label="البصمة المسجّلة" value={recorded} hint="حُسبت وقت الرفع" />
            <DigestLine label="المحسوبة من القرص" value={computed} hint="أعاد الخادم حسابها الآن" />
            {local && (
              <DigestLine
                label="البصمة المحلية"
                value={local}
                hint={`حُسبت من «${pickedName ?? 'الملف المختار'}» على جهازك`}
              />
            )}
          </>
        ) : null}
      </div>

      {result && (
        <div className="flex flex-col gap-1.5 rounded-lg border border-border bg-surface p-3">
          <ComparisonLine label="المسجّلة مقابل المحسوبة من القرص" isMatch={result.isMatch} />
          <ComparisonLine label="المسجّلة مقابل المحلية" isMatch={matchesLocal(recorded)} />
          <ComparisonLine label="المحسوبة من القرص مقابل المحلية" isMatch={matchesLocal(computed)} />
          {!local && (
            <p className="text-xs text-muted">
              اختر نسخة الملف من جهازك لإضافة مقارنة محلية.
            </p>
          )}
        </div>
      )}

      {!verifyQuery.isPending && !verifyQuery.error && (
        <div className="flex flex-col gap-1">
          <Input
            id="attachment-verify-file"
            type="file"
            dir="ltr"
            onChange={(event) => void pickFile(event.target.files?.[0] ?? null)}
            disabled={checksum.isHashing}
            className="text-xs"
          />
          {checksum.isHashing && (
            <p className="flex items-center gap-1.5 text-xs text-muted">
              <Loader2 className="size-3.5 animate-spin" aria-hidden />
              جارٍ حساب البصمة المحلية…
            </p>
          )}
          {checksum.error && <p className="text-xs text-danger">{checksum.error}</p>}
        </div>
      )}

      <p className="text-xs text-muted">
        التحقق يقرأ النسخة المخزنة فقط؛ لا يُعدّل الملف ولا يحذفه، والملف المؤرشف يبقى قابلًا للتحقق.
      </p>
    </Dialog>
  )
}
