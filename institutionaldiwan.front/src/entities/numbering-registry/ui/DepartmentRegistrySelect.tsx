import { useEffect } from 'react'
import { Select } from '@/shared/ui'
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
          isLoading ? 'جارٍ التحميل…' : options.length === 0 ? 'لا يوجد سجل نشط' : 'اختر السجل…'
        }
        aria-label="السجل"
      />
      {isEmpty && !hideEmptyHint && (
        <p className="text-xs text-danger">
          لا يوجد لدائرتك سجل ترقيم نشط؛ يضيفه المسؤول من صفحة «الترقيم».
        </p>
      )}
    </>
  )
}
