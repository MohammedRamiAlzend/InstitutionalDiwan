import type { NumberingRegistryResponse } from '@/entities/numbering-registry'
import { RegistryScopeCard, renderNumberPattern } from '@/entities/numbering-registry'
import { formatNumber, t } from '@/shared/lib'
import { Badge, Button, Dialog, Label } from '@/shared/ui'

interface RegistryDetailsDialogProps {
  registry: NumberingRegistryResponse
  departmentName?: string | null
  documentTypeLabel?: string | null
  directionLabel?: string | null
  /** Other active registries that share this exact scope. */
  duplicateCodes?: readonly string[]
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Read-only registry summary: next expected number, scope conflicts, and yearly counters. */
export function RegistryDetailsDialog({
  registry,
  departmentName,
  documentTypeLabel,
  directionLabel,
  duplicateCodes = [],
  open,
  onOpenChange,
}: RegistryDetailsDialogProps) {
  const currentYear = new Date().getFullYear()
  const currentCounter = registry.counters.find(
    (counter) => counter.numberingYear === currentYear,
  )
  const nextSequence = (currentCounter?.lastValue ?? 0) + 1
  const nextNumber = renderNumberPattern(registry.formatPattern, {
    numberingYear: currentYear,
    sequenceValue: nextSequence,
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('numbering.details.title', { code: registry.code })}
      description={registry.nameAr}
      size="lg"
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          <Label k="common.action.close" />
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        {duplicateCodes.length > 0 && (
          <div
            role="status"
            className="flex flex-col gap-1 rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-xs text-warning"
          >
            <p className="font-semibold"><Label k="numbering.details.duplicate-title" /></p>
            <p>{t('numbering.details.duplicate-list', { codes: duplicateCodes.join('، ') })}</p>
            <p><Label k="numbering.details.duplicate-hint" /></p>
          </div>
        )}

        <section className="flex flex-col gap-2 rounded-xl border border-border bg-surface-muted/40 p-4">
          <h3 className="text-xs font-semibold text-muted"><Label k="numbering.details.next-title" /></h3>
          <div className="flex flex-wrap items-center gap-2">
            <code
              dir="ltr"
              className="rounded-lg bg-surface px-3 py-1.5 font-mono text-base font-semibold text-foreground"
            >
              {nextNumber}
            </code>
            <Badge variant="outline">
              <Label k="numbering.details.year" /> <span dir="ltr">{currentYear}</span>
            </Badge>
            <Badge variant="outline">
              <Label k="numbering.details.sequence" /> <span dir="ltr">{formatNumber(nextSequence)}</span>
            </Badge>
          </div>
          <p className="text-xs text-muted">
            <Label k="numbering.details.next-hint" />
          </p>
        </section>

        <RegistryScopeCard
          registry={registry}
          departmentName={departmentName}
          documentTypeLabel={documentTypeLabel}
          directionLabel={directionLabel}
        />

        <p className="rounded-lg bg-surface-muted px-3 py-2 text-xs text-muted">
          <Label k="numbering.details.scope-hint" />
        </p>
      </div>
    </Dialog>
  )
}