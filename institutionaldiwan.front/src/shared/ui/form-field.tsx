import { useId, type ReactNode } from 'react'
import { cn } from '@/shared/lib'

interface FormFieldProps {
  label: string
  required?: boolean
  error?: string
  htmlFor?: string
  children: ReactNode
  className?: string
}

/** Label + control + Arabic error message wrapper; integrates with react-hook-form. */
export function FormField({
  label,
  required,
  error,
  htmlFor,
  children,
  className,
}: FormFieldProps) {
  const autoId = useId()
  const id = htmlFor ?? autoId

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="text-sm font-medium text-foreground">
        {label}
        {required && <span className="text-danger"> *</span>}
      </label>
      {children}
      {error && (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  )
}
