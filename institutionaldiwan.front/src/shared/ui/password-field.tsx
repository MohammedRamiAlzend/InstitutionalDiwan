import { useState } from 'react'
import type { UseFormRegisterReturn } from 'react-hook-form'
import { Eye, EyeOff } from 'lucide-react'
import { FormField } from './form-field'
import { Input } from './input'

interface PasswordFieldProps {
  id: string
  label: string
  autoComplete: string
  placeholder?: string
  error?: string
  registration: UseFormRegisterReturn
}

/** Password input with a per-field show/hide toggle; used by every password form. */
export function PasswordField({
  id,
  label,
  autoComplete,
  placeholder,
  error,
  registration,
}: PasswordFieldProps) {
  const [visible, setVisible] = useState(false)

  return (
    <FormField label={label} required htmlFor={id} error={error}>
      <Input
        id={id}
        type={visible ? 'text' : 'password'}
        autoComplete={autoComplete}
        placeholder={placeholder}
        invalid={Boolean(error)}
        endIcon={
          <button
            type="button"
            onClick={() => setVisible((value) => !value)}
            className="cursor-pointer p-1 hover:text-foreground"
            aria-label={visible ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'}
          >
            {visible ? (
              <EyeOff className="size-4" aria-hidden />
            ) : (
              <Eye className="size-4" aria-hidden />
            )}
          </button>
        }
        {...registration}
      />
    </FormField>
  )
}
