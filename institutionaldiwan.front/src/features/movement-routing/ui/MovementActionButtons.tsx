import type { MovementAction } from '@/entities/movement'
import { Button } from '@/shared/ui'
import {
  DESTRUCTIVE_ACTIONS,
  MOVEMENT_ACTION_ICONS,
  MOVEMENT_ACTION_LABELS,
} from '../model/constants'

interface MovementActionButtonsProps {
  actions: readonly MovementAction[]
  onAction: (action: MovementAction) => void
  disabled?: boolean
}

/** Inline action cluster for the document movements tab. */
export function MovementActionButtons({
  actions,
  onAction,
  disabled,
}: MovementActionButtonsProps) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      {actions.map((action) => {
        const Icon = MOVEMENT_ACTION_ICONS[action]
        const danger = DESTRUCTIVE_ACTIONS.includes(action)

        return (
          <Button
            key={action}
            type="button"
            size="sm"
            variant={danger ? 'outline' : 'primary'}
            className={danger ? 'text-danger' : undefined}
            disabled={disabled}
            onClick={() => onAction(action)}
          >
            <Icon
              className={action === 'send' || action === 'redirect' ? 'size-4 rtl:-scale-x-100' : 'size-4'}
              aria-hidden
            />
            {MOVEMENT_ACTION_LABELS[action]}
          </Button>
        )
      })}
    </div>
  )
}
