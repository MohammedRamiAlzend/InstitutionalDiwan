import type { NumberingRegistryResponse } from '@/entities/numbering-registry'
import { RegistryScopeCard } from '@/entities/numbering-registry'
import { Button, Dialog } from '@/shared/ui'

interface RegistryDetailsDialogProps {
  registry: NumberingRegistryResponse
  departmentName?: string | null
  documentTypeLabel?: string | null
  directionLabel?: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Read-only registry summary with scope and per-year counters. */
export function RegistryDetailsDialog({
  registry,
  departmentName,
  documentTypeLabel,
  directionLabel,
  open,
  onOpenChange,
}: RegistryDetailsDialogProps) {
  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={`سجل ${registry.code}`}
      description={registry.nameAr}
      size="lg"
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          إغلاق
        </Button>
      }
    >
      <RegistryScopeCard
        registry={registry}
        departmentName={departmentName}
        documentTypeLabel={documentTypeLabel}
        directionLabel={directionLabel}
      />
    </Dialog>
  )
}
