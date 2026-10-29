import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { FileText, RefreshCw } from 'lucide-react'
import { useDepartmentOptions } from '@/entities/department'
import { NumberStatusBadge } from '@/entities/document'
import {
  useDepartmentRegister,
  type DepartmentRegisterLineResponse,
} from '@/entities/numbering-registry'
import { toArabicErrorMessage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { formatDate, formatNumber } from '@/shared/lib'
import { Badge, Button, FormField, Input, Select, Skeleton } from '@/shared/ui'

/** Read-only view of a department's own register book, optionally per numbering year. */
export function DepartmentRegisterSection() {
  const currentYear = String(new Date().getFullYear())
  const [departmentId, setDepartmentId] = useState('')
  const [year, setYear] = useState(currentYear)

  const departmentsQuery = useDepartmentOptions()
  const numberingYear = /^\d{4}$/.test(year.trim()) ? Number(year.trim()) : null

  const registerQuery = useDepartmentRegister(
    departmentId || null,
    numberingYear,
  )

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.code} — ${department.nameAr}`,
        })),
    [departmentsQuery.data],
  )

  const lines = registerQuery.data ?? []

  return (
    <section className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex flex-col gap-1">
          <h2 className="text-base font-semibold text-foreground">سجل الدائرة</h2>
          <p className="max-w-2xl text-xs text-muted">
            الأرقام التي سجّلتها الدوائر في سجلاتها الخاصة، مع رقم الديوان المركزي المقابل.
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => void registerQuery.refetch()}
          disabled={!departmentId || registerQuery.isFetching}
        >
          <RefreshCw
            className={registerQuery.isFetching ? 'size-4 animate-spin' : 'size-4'}
            aria-hidden
          />
          تحديث
        </Button>
      </div>

      <div className="grid gap-3 sm:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]">
        <FormField label="الدائرة" htmlFor="register-department">
          <Select
            id="register-department"
            value={departmentId || undefined}
            onValueChange={setDepartmentId}
            options={departmentOptions}
            disabled={departmentsQuery.isLoading}
            placeholder={
              departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر الدائرة…'
            }
            aria-label="الدائرة"
          />
        </FormField>
        <FormField label="سنة الترقيم" htmlFor="register-year">
          <Input
            id="register-year"
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1900}
            max={2200}
            value={year}
            onChange={(event) => setYear(event.target.value)}
            placeholder="كل السنوات"
          />
        </FormField>
      </div>

      {!departmentId ? (
        <p className="rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted">
          اختر دائرة لعرض أرقامها المسجّلة.
        </p>
      ) : registerQuery.isLoading ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : registerQuery.isError ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{toArabicErrorMessage(registerQuery.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void registerQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : lines.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted">
          لا توجد أرقام مسجّلة لهذه الدائرة في السنة المحددة.
        </p>
      ) : (
        <div className="overflow-hidden rounded-lg border border-border">
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <caption className="sr-only">سجل أرقام الدائرة</caption>
              <thead className="bg-surface-muted">
                <tr>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    الوثيقة
                  </th>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    رقم الديوان
                  </th>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    رقم الدائرة
                  </th>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    تاريخ التسجيل
                  </th>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    الحالة
                  </th>
                  <th scope="col" className="px-3 py-2.5 text-start text-xs font-semibold text-muted">
                    مرات التسجيل
                  </th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line: DepartmentRegisterLineResponse) => (
                  <tr key={line.documentNumberId} className="border-t border-border">
                    <td className="px-3 py-2.5">
                      <Link
                        to={`${ROUTES.DOCUMENT_DETAIL(line.documentId)}?tab=numbers`}
                        className="inline-flex items-center gap-1.5 text-sm text-accent transition-colors hover:underline"
                      >
                        <FileText className="size-3.5" aria-hidden />
                        <span className="line-clamp-1 max-w-72">{line.documentTitle}</span>
                      </Link>
                    </td>
                    <td className="px-3 py-2.5">
                      <span dir="ltr" className="text-xs text-muted">
                        {line.centralNumberText ?? '—'}
                      </span>
                    </td>
                    <td className="px-3 py-2.5">
                      <code dir="ltr" className="text-xs text-foreground">
                        {line.registryNumberText}
                      </code>
                    </td>
                    <td className="px-3 py-2.5">
                      <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
                        {formatDate(line.registryDate)}
                      </span>
                    </td>
                    <td className="px-3 py-2.5">
                      <NumberStatusBadge statusId={line.statusId} />
                    </td>
                    <td className="px-3 py-2.5">
                      <Badge variant="outline">{formatNumber(line.registrationCount)}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  )
}
