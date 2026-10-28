import * as CheckboxPrimitive from '@radix-ui/react-checkbox'
import { Check, Minus } from 'lucide-react'
import { useId } from 'react'
import { cn } from '@/shared/lib'

interface CheckboxProps {
  checked: boolean | 'indeterminate'
  onCheckedChange: (checked: boolean) => void
  label?: string
  description?: string
  disabled?: boolean
  id?: string
  className?: string
}

/** Accessible checkbox with an Arabic label and optional helper text. */
export function Checkbox({
  checked,
  onCheckedChange,
  label,
  description,
  disabled,
  id,
  className,
}: CheckboxProps) {
  const autoId = useId()
  const checkboxId = id ?? autoId

  return (
    <div className={cn('flex items-start gap-2.5', className)}>
      <CheckboxPrimitive.Root
        id={checkboxId}
        checked={checked}
        disabled={disabled}
        onCheckedChange={(value) => onCheckedChange(value === true)}
        className={cn(
          'mt-0.5 flex size-4.5 shrink-0 items-center justify-center rounded-sm border transition-colors',
          'border-border bg-surface text-accent-contrast',
          'data-[state=checked]:border-accent data-[state=checked]:bg-accent',
          'data-[state=indeterminate]:border-accent data-[state=indeterminate]:bg-accent',
          'disabled:cursor-not-allowed disabled:opacity-60',
        )}
      >
        <CheckboxPrimitive.Indicator className="flex items-center justify-center">
          {checked === 'indeterminate' ? (
            <Minus className="size-3" aria-hidden />
          ) : (
            <Check className="size-3" aria-hidden />
          )}
        </CheckboxPrimitive.Indicator>
      </CheckboxPrimitive.Root>

      {(label || description) && (
        <div className="flex min-w-0 flex-col gap-0.5">
          {label && (
            <label htmlFor={checkboxId} className="cursor-pointer text-sm text-foreground">
              {label}
            </label>
          )}
          {description && <span className="text-xs text-muted">{description}</span>}
        </div>
      )}
    </div>
  )
}
