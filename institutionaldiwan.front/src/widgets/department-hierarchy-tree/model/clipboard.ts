import { toast } from 'sonner'
import { t } from '@/shared/lib'

/** Copies a department code or id and reports the result in Arabic. */
export async function copyDepartmentValue(value: string, label: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(value)
    toast.success(t('department.toast.copied', { label }))
  } catch {
    toast.error(t('department.toast.copy-failed'))
  }
}
