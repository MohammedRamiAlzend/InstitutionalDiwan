import { NameWithId } from '@/shared/ui'
import { useEmployeeOptionById } from '../model/options'

interface EmployeeLabelProps {
  employeeId: string | null | undefined
  className?: string
}

/** An employee's Arabic full name with the short id; the bare id where names are not readable. */
export function EmployeeLabel({ employeeId, className }: EmployeeLabelProps) {
  const query = useEmployeeOptionById(employeeId)

  if (!employeeId) return <span>—</span>
  return <NameWithId name={query.data?.fullNameAr} id={employeeId} className={className} />
}
