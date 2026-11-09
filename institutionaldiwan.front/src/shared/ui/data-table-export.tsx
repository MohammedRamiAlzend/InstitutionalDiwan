import { useState, type RefObject } from 'react'
import { Download, FileSpreadsheet, FileText } from 'lucide-react'
import { toast } from 'sonner'
import { httpClient, toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, formatNumber, saveBlobAsFile } from '@/shared/lib'
import { Button } from './button'
import { DropdownMenu } from './dropdown-menu'
import type { MenuEntry } from './menu'
import { PrintSheet } from './print-sheet'

const BLOCK_BREAK = '\u0000'
const VALUE_SEPARATOR = ' · '
/** Invisible on screen, but a cell holding them would not match a search or sort in Excel. */
const BIDI_MARKS = /[‎‏‪-‮⁦-⁩]/g

/** The text a person sees in a cell: screen-reader-only labels and icons are left out. */
function readCellText(cell: HTMLTableCellElement): string {
  let text = ''

  const walk = (node: Node) => {
    if (node.nodeType === Node.TEXT_NODE) {
      text += node.textContent ?? ''
      return
    }
    if (!(node instanceof HTMLElement)) return
    if (node.classList.contains('sr-only') || node.getAttribute('aria-hidden') === 'true') return

    const display = window.getComputedStyle(node).display
    if (display === 'none') return

    const isBlock = !display.startsWith('inline')
    if (isBlock) text += BLOCK_BREAK
    node.childNodes.forEach(walk)
    if (isBlock) text += BLOCK_BREAK
  }

  cell.childNodes.forEach(walk)

  return text
    .split(BLOCK_BREAK)
    .map((part) => part.replace(BIDI_MARKS, '').replace(/\s+/g, ' ').trim())
    .filter(Boolean)
    .join(VALUE_SEPARATOR)
}

function readRows(table: HTMLTableElement, columnCount: number): string[][] {
  return Array.from(table.tBodies[0]?.rows ?? [], (row) =>
    Array.from(row.cells)
      .slice(0, columnCount)
      .map(readCellText),
  )
}

function safeFileName(title: string): string {
  const date = new Date().toISOString().slice(0, 10)
  return `${title.replace(/[\\/:*?"<>|]/g, '-').trim()} ${date}`
}

interface TableSnapshot {
  rows: string[][]
  printedAt: string
}

interface DataTableExportProps {
  tableRef: RefObject<HTMLTableElement | null>
  title: string
  /** Headers of the columns on screen, in order; a trailing actions cell is not among them. */
  headers: readonly string[]
  shownRows: number
  totalRows: number
  disabled: boolean
}

/**
 * The «تصدير» menu of every table. It reads the table as it is on screen, so the file holds the
 * visible columns and the current filter, order and page, with names already resolved.
 */
export function DataTableExport({
  tableRef,
  title,
  headers,
  shownRows,
  totalRows,
  disabled,
}: DataTableExportProps) {
  const [isSaving, setIsSaving] = useState(false)
  const [snapshot, setSnapshot] = useState<TableSnapshot | null>(null)

  const exportExcel = async () => {
    const table = tableRef.current
    if (!table) return

    setIsSaving(true)
    try {
      const { data } = await httpClient.post<Blob>(
        '/exports/table',
        { title, headers, rows: readRows(table, headers.length) },
        { responseType: 'blob' },
      )
      saveBlobAsFile(data, `${safeFileName(title)}.xlsx`)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  const exportPdf = () => {
    const table = tableRef.current
    if (!table) return
    setSnapshot({
      rows: readRows(table, headers.length),
      printedAt: formatDateTime(new Date().toISOString()),
    })
  }

  const entries: MenuEntry[] = [
    {
      type: 'label',
      label: `الصفوف المعروضة: ${formatNumber(shownRows)} من ${formatNumber(totalRows)}`,
    },
    {
      id: 'export-excel',
      label: 'Excel',
      icon: <FileSpreadsheet className="size-4" aria-hidden />,
      onSelect: () => void exportExcel(),
    },
    {
      id: 'export-pdf',
      label: 'PDF',
      icon: <FileText className="size-4" aria-hidden />,
      onSelect: exportPdf,
    },
  ]

  return (
    <>
      <DropdownMenu
        trigger={
          <Button type="button" variant="outline" size="sm" className="h-10" disabled={disabled || isSaving}>
            <Download className="size-4" aria-hidden />
            {isSaving ? 'جارٍ التصدير…' : 'تصدير'}
          </Button>
        }
        entries={entries}
        label="تصدير الجدول"
      />
      {snapshot && (
        <PrintSheet fileName={safeFileName(title)} orientation="landscape" onDone={() => setSnapshot(null)}>
          <header className="mb-3 flex items-baseline justify-between gap-4 border-b-2 border-accent pb-2">
            <h1 className="text-lg font-bold text-foreground">{title}</h1>
            <p className="text-xs text-muted">
              <span dir="ltr">{snapshot.printedAt}</span> · عدد الصفوف:{' '}
              <span dir="ltr">{formatNumber(snapshot.rows.length)}</span>
            </p>
          </header>
          <table className="w-full border-collapse text-[11px] text-foreground">
            <thead>
              <tr className="bg-surface-muted">
                {headers.map((header) => (
                  <th key={header} className="border border-border px-2 py-1.5 text-start font-semibold">
                    {header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {snapshot.rows.map((row, rowIndex) => (
                <tr key={rowIndex}>
                  {row.map((value, cellIndex) => (
                    <td key={cellIndex} className="border border-border px-2 py-1 align-top">
                      {value}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </PrintSheet>
      )}
    </>
  )
}
