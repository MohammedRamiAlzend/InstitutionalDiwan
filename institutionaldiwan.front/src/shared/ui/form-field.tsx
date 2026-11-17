import { useId, type ReactNode } from 'react'
import type { LabelKey } from '@/shared/config'
import { cn, useTutorial } from '@/shared/lib'
import { GuideChip } from './guide'
import { Label } from './label'

interface FormFieldProps {
  label?: string
  /** Catalog key for the label; takes priority over `label` and is editable in place. */
  labelKey?: LabelKey
  required?: boolean
  error?: string
  htmlFor?: string
  /** Explains this field in tutorial mode; the control itself keeps the label. */
  guideId?: string
  /** Quiet explanation under the control; hidden while an error is shown in its place. */
  hint?: string
  children: ReactNode
  className?: string
}

/** Label + control + Arabic error message wrapper; integrates with react-hook-form. */
export function FormField({
  label,
  labelKey,
  required,
  error,
  htmlFor,
  guideId,
  hint,
  children,
  className,
}: FormFieldProps) {
  const autoId = useId()
  const id = htmlFor ?? autoId
  const showGuide = useTutorial() === 'on' && guideId !== undefined

  const labelElement = (
    <label htmlFor={id} className="text-sm font-medium text-foreground">
      {labelKey ? <Label k={labelKey} /> : label}
      {required && <span className="text-danger"> *</span>}
    </label>
  )

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      {showGuide ? (
        <div className="flex items-center gap-1.5">
          {labelElement}
          <GuideChip id={guideId} />
        </div>
      ) : (
        labelElement
      )}
      {children}
      {hint && !error && <p className="text-xs text-muted">{hint}</p>}
      {error && (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  )
}
