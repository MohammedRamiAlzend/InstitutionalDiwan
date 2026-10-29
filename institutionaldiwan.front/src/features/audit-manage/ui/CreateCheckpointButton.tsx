import { useState } from 'react'
import { Stamp } from 'lucide-react'
import { Button } from '@/shared/ui'
import { CreateCheckpointDialog } from './CreateCheckpointDialog'

interface CreateCheckpointButtonProps {
  disabled?: boolean
}

/** Toolbar entry for sealing a checkpoint; the dialog owns the confirmation. */
export function CreateCheckpointButton({ disabled }: CreateCheckpointButtonProps) {
  const [open, setOpen] = useState(false)

  return (
    <>
      <Button type="button" variant="outline" disabled={disabled} onClick={() => setOpen(true)}>
        <Stamp className="size-4" aria-hidden />
        نقطة تحقق
      </Button>
      {open && <CreateCheckpointDialog open onOpenChange={setOpen} />}
    </>
  )
}
