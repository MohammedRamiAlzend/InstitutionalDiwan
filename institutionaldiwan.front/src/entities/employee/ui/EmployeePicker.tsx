import { useState } from 'react'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber, formatShortId, t, useDebouncedValue } from '@/shared/lib'
import { Button, Input, Label, Select } from '@/shared/ui'
import { useEmployeeOptionById as useEmployeeById, useEmployeeOptions as useEmployees } from '../model/options'

interface EmployeePickerProps {
  value: string | null
  onValueChange: (value: string | null) => void
  label: string
  noneLabel: string
  disabled?: boolean
  invalid?: boolean
}

const NONE = 'none'

export function EmployeePicker({
  value,
  onValueChange,
  label,
  noneLabel,
  disabled = false,
  invalid = false,
}: EmployeePickerProps) {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search, 300)
  const query = useEmployees(
    { page, pageSize: 20, search: debouncedSearch },
    { enabled: !disabled },
  )
  const selectedQuery = useEmployeeById(value, { enabled: !disabled })
  const options = (query.data?.items ?? []).map((employee) => ({
    value: employee.employeeId,
    label: employee.employeeNumber
      ? `${employee.fullNameAr} (${employee.employeeNumber})`
      : employee.fullNameAr,
    disabled: !employee.isActive && employee.employeeId !== value,
  }))
  if (value && !options.some((option) => option.value === value)) {
    options.unshift({
      value,
      label: selectedQuery.data?.fullNameAr ?? t('employee.picker.fallback', { id: formatShortId(value) }),
      disabled: false,
    })
  }

  return (
    <div className="flex flex-col gap-2">
      {!disabled && (
        <Input
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(1)
          }}
          placeholder={t('employee.picker.search-placeholder')}
          aria-label={t('employee.picker.search-aria', { label })}
        />
      )}
      <Select
        value={value ?? NONE}
        onValueChange={(id) => onValueChange(id === NONE ? null : id)}
        options={[{ value: NONE, label: noneLabel }, ...options]}
        disabled={disabled || query.isLoading}
        invalid={invalid}
        aria-label={label}
      />
      {!disabled && query.error && (
        <div role="alert" className="flex items-center justify-between gap-2 text-xs text-danger">
          <span>{toArabicErrorMessage(query.error)}</span>
          <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>
            <Label k="common.action.retry" />
          </Button>
        </div>
      )}
      {!disabled && query.data && (
        <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-muted" aria-live="polite">
          <span>{t('employee.picker.results', { count: formatNumber(query.data.totalCount), page: formatNumber(page) })}</span>
          <div className="flex gap-1">
            <Button type="button" variant="ghost" size="sm" disabled={page === 1 || query.isFetching} onClick={() => setPage(page - 1)}>
              <Label k="employee.picker.prev" />
            </Button>
            <Button type="button" variant="ghost" size="sm" disabled={!query.data.hasNext || query.isFetching} onClick={() => setPage(page + 1)}>
              <Label k="employee.picker.next" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
