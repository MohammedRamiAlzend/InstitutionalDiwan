import { useEffect } from 'react'
import { t } from '@/shared/lib'
import { Label, Select } from '@/shared/ui'
import type { DepartmentRegistryOptions } from '../model/departmentRegistries'

interface DepartmentRegistrySelectProps {
  registries: DepartmentRegistryOptions
  value: string
  onChange: (registryId: string) => void
  disabled?: boolean
  invalid?: boolean
  hideEmptyHint?: boolean
}

/**
 * Chooses the department register a number is written in. A single register needs no choice, so it
 * is preselected and locked; a value that is no longer one of the options is cleared.
 */
export function DepartmentRegistrySelect({
  registries,
  value,
  onChange,
  disabled,
  invalid,
  hideEmptyHint,
}: DepartmentRegistrySelectProps) {
  const { options, isLoading, isEmpty } = registries

  useEffect(() => {
    if (options.length === 1 && value !== options[0].value) {
      onChange(options[0].value)
    } else if (options.length > 1 && value && !options.some((option) => option.value === value)) {
      onChange('')
    }
  }, [options, value, onChange])

  return (
    <>
      <Select
        value={value || undefined}
        onValueChange={onChange}
        options={options}
        disabled={disabled || isLoading || options.length <= 1}
        invalid={invalid}
        placeholder={
          isLoading
            ? t('numbering.loading')
            : options.length === 0
              ? t('numbering.select.empty')
              : t('numbering.select.placeholder')
        }
        aria-label={t('numbering.select.aria')}
      />
      {isEmpty && !hideEmptyHint && (
        <p className="text-xs text-danger">
          <Label k="numbering.select.empty-hint" />
        </p>
      )}
    </>
  )
}
