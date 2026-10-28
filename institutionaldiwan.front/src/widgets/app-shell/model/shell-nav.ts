import type { LucideIcon } from 'lucide-react'
import {
  Building2,
  FileText,
  Hash,
  IdCard,
  Inbox,
  LayoutDashboard,
  ScrollText,
  ShieldCheck,
  Users,
} from 'lucide-react'
import { Permissions, ROUTES, type PermissionCode } from '@/shared/config'

export interface ShellNavItem {
  id: string
  label: string
  to: string
  icon: LucideIcon
  /** Hidden entirely when the caller lacks this permission. */
  permission?: PermissionCode
  /** False until the page slice exists; rendered disabled with an Arabic hint. */
  available: boolean
}

export interface ShellNavGroup {
  id: string
  label: string
  items: readonly ShellNavItem[]
}

export const SHELL_NAV_GROUPS: readonly ShellNavGroup[] = [
  {
    id: 'overview',
    label: 'نظرة عامة',
    items: [
      {
        id: 'dashboard',
        label: 'لوحة التحكم',
        to: ROUTES.DASHBOARD,
        icon: LayoutDashboard,
        available: true,
      },
    ],
  },
  {
    id: 'correspondence',
    label: 'المراسلات',
    items: [
      {
        id: 'documents',
        label: 'الوثائق',
        to: ROUTES.DOCUMENTS,
        icon: FileText,
        permission: Permissions.DocumentView,
        available: false,
      },
      {
        id: 'movements',
        label: 'الحركات',
        to: ROUTES.MOVEMENTS,
        icon: Inbox,
        permission: Permissions.DocumentView,
        available: false,
      },
    ],
  },
  {
    id: 'organization',
    label: 'التنظيم',
    items: [
      {
        id: 'departments',
        label: 'الأقسام',
        to: ROUTES.DEPARTMENTS,
        icon: Building2,
        permission: Permissions.DepartmentManage,
        available: true,
      },
    ],
  },
  {
    id: 'access',
    label: 'إدارة الوصول',
    items: [
      {
        id: 'users',
        label: 'المستخدمون',
        to: ROUTES.USERS,
        icon: Users,
        permission: Permissions.UserManage,
        available: true,
      },
      {
        id: 'roles',
        label: 'الأدوار والصلاحيات',
        to: ROUTES.ROLES,
        icon: ShieldCheck,
        permission: Permissions.RoleManage,
        available: true,
      },
      {
        id: 'employees',
        label: 'الموظفون',
        to: ROUTES.EMPLOYEES,
        icon: IdCard,
        permission: Permissions.EmployeeManage,
        available: true,
      },
    ],
  },
  {
    id: 'governance',
    label: 'الرقابة والتدقيق',
    items: [
      {
        id: 'numbering',
        label: 'الترقيم',
        to: ROUTES.NUMBERING,
        icon: Hash,
        permission: Permissions.NumberingView,
        available: false,
      },
      {
        id: 'audit',
        label: 'سجل التدقيق',
        to: ROUTES.AUDIT,
        icon: ScrollText,
        permission: Permissions.AuditView,
        available: false,
      },
    ],
  },
]

/** Keeps only groups that still have at least one permitted item. */
export function filterNavGroups(
  permissions: readonly PermissionCode[],
): ShellNavGroup[] {
  return SHELL_NAV_GROUPS.map((group) => ({
    ...group,
    items: group.items.filter(
      (item) => !item.permission || permissions.includes(item.permission),
    ),
  })).filter((group) => group.items.length > 0)
}

export function findNavItem(pathname: string): ShellNavItem | undefined {
  for (const group of SHELL_NAV_GROUPS) {
    for (const item of group.items) {
      const matches =
        item.to === '/'
          ? pathname === '/'
          : pathname === item.to || pathname.startsWith(`${item.to}/`)
      if (matches) return item
    }
  }
  return undefined
}
