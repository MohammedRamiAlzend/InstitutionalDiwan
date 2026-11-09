import type { ReactNode } from 'react'
import { formatDate, formatDateTime } from '@/shared/lib'

export function PrintSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="mt-5">
      <h2 className="mb-2 border-s-4 border-accent ps-2 text-sm font-bold text-foreground">{title}</h2>
      {children}
    </section>
  )
}

export interface PrintFact {
  label: string
  value: ReactNode
}

/** Label and value pairs in `columns` columns; used for the document's own data. */
export function PrintFacts({ facts, columns = 3 }: { facts: readonly PrintFact[]; columns?: 2 | 3 }) {
  return (
    <dl className={`grid gap-x-4 gap-y-2 rounded-md border border-border p-3 ${columns === 3 ? 'grid-cols-3' : 'grid-cols-2'}`}>
      {facts.map((fact) => (
        <div key={fact.label} className="print-keep flex flex-col">
          <dt className="text-[10px] text-muted">{fact.label}</dt>
          <dd className="text-xs text-foreground">{fact.value}</dd>
        </div>
      ))}
    </dl>
  )
}

export function PrintTable({ headers, rows }: { headers: readonly string[]; rows: readonly ReactNode[][] }) {
  return (
    <table className="w-full border-collapse text-[11px] text-foreground">
      <thead>
        <tr className="bg-surface-muted">
          {headers.map((header) => (
            <th key={header} className="border border-border px-2 py-1 text-start font-semibold">
              {header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, rowIndex) => (
          <tr key={rowIndex}>
            {row.map((cell, cellIndex) => (
              <td key={cellIndex} className="border border-border px-2 py-1 align-top">
                {cell}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export function PrintDate({ value }: { value: string | null | undefined }) {
  return value ? <span dir="ltr">{formatDate(value)}</span> : <span>—</span>
}

export function PrintDateTime({ value }: { value: string | null | undefined }) {
  return value ? <span dir="ltr">{formatDateTime(value)}</span> : <span>—</span>
}

export function PrintLatin({ value }: { value: string | null | undefined }) {
  return value ? <span dir="ltr">{value}</span> : <span>—</span>
}
