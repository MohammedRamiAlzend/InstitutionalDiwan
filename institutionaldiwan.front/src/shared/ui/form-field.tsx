import { useId, type ReactNode } from 'react'
import { cn, useTutorial } from '@/shared/lib'
import { GuideChip } from './guide'

interface FormFieldProps {
  label: string
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
      {label}
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
