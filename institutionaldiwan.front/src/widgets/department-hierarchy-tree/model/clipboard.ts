import { toast } from 'sonner'

/** Copies a department code or id and reports the result in Arabic. */
export async function copyDepartmentValue(value: string, label: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(value)
    toast.success(`تم نسخ ${label}.`)
  } catch {
    toast.error('تعذّر النسخ إلى الحافظة.')
  }
}
