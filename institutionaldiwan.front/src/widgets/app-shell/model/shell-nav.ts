import type { LucideIcon } from 'lucide-react'
import {
  Archive,
  BookMarked,
  Bug,
  Contact,
  Building2,
  FileText,
  FolderOpen,
  Hash,
  IdCard,
  Library,
  ListPlus,
  Inbox,
  Bell,
  LayoutDashboard,
  Scale,
  ScrollText,
  Settings2,
  ShieldCheck,
  Users,
} from 'lucide-react'
import { Permissions, ROUTES, type LabelKey, type PermissionCode } from '@/shared/config'

export interface ShellNavItem {
  id: string
  /** Catalog key; the renderer resolves it through the labels store so renames apply live. */
  labelKey: LabelKey
  to: string
  icon: LucideIcon
  /** Hidden entirely when the caller lacks this permission. */
  permission?: PermissionCode
  /** False until the page slice exists; rendered disabled with an Arabic hint. */
  available: boolean
}

export interface ShellNavGroup {
  id: string
  labelKey: LabelKey
  items: readonly ShellNavItem[]
}

export const SHELL_NAV_GROUPS: readonly ShellNavGroup[] = [
  {
    id: 'overview',
    labelKey: 'nav.group.overview',
    items: [
      {
        id: 'dashboard',
        labelKey: 'nav.dashboard',
        to: ROUTES.DASHBOARD,
        icon: LayoutDashboard,
        available: true,
      },
      {
        id: 'notifications',
        labelKey: 'nav.notifications',
        to: ROUTES.NOTIFICATIONS,
        icon: Bell,
        available: true,
      },
    ],
  },
  {
    id: 'correspondence',
    labelKey: 'nav.group.correspondence',
    items: [
      {
        id: 'documents',
        labelKey: 'nav.documents',
        to: ROUTES.DOCUMENTS,
        icon: FileText,
        permission: Permissions.DocumentView,
        available: true,
      },
      {
        id: 'movements',
        labelKey: 'nav.movements',
        to: ROUTES.MOVEMENTS,
        icon: Inbox,
        permission: Permissions.DocumentView,
        available: true,
      },
      {
        id: 'decisions',
        labelKey: 'nav.decisions',
        to: ROUTES.DECISIONS,
        icon: Scale,
        permission: Permissions.DocumentView,
        available: true,
      },
      {
        id: 'my-registers',
        labelKey: 'nav.my-registers',
        to: ROUTES.MY_REGISTERS,
        icon: BookMarked,
        permission: Permissions.NumberingView,
        available: true,
      },
      {
        id: 'persons',
        labelKey: 'nav.persons',
        to: ROUTES.PERSONS,
        icon: Contact,
        permission: Permissions.PersonView,
        available: true,
      },
      {
        id: 'archive',
        labelKey: 'nav.archive',
        to: ROUTES.ARCHIVE,
        icon: Archive,
        permission: Permissions.DocumentView,
        available: true,
      },
      {
        id: 'drive',
        labelKey: 'nav.drive',
        to: ROUTES.DRIVE,
        icon: FolderOpen,
        permission: Permissions.DriveUse,
        available: true,
      },
    ],
  },
  {
    id: 'organization',
    labelKey: 'nav.group.organization',
    items: [
      {
        id: 'departments',
        labelKey: 'nav.departments',
        to: ROUTES.DEPARTMENTS,
        icon: Building2,
        permission: Permissions.DepartmentManage,
        available: true,
      },
      {
        id: 'institution-settings',
        labelKey: 'nav.institution-settings',
        to: ROUTES.INSTITUTION_SETTINGS,
        icon: Settings2,
        permission: Permissions.DepartmentManage,
        available: true,
      },
    ],
  },
  {
    id: 'access',
    labelKey: 'nav.group.access',
    items: [
      {
        id: 'users',
        labelKey: 'nav.users',
        to: ROUTES.USERS,
        icon: Users,
        permission: Permissions.UserManage,
        available: true,
      },
      {
        id: 'roles',
        labelKey: 'nav.roles',
        to: ROUTES.ROLES,
        icon: ShieldCheck,
        permission: Permissions.RoleManage,
        available: true,
      },
      {
        id: 'employees',
        labelKey: 'nav.employees',
        to: ROUTES.EMPLOYEES,
        icon: IdCard,
        permission: Permissions.EmployeeManage,
        available: true,
      },
    ],
  },
  {
    id: 'governance',
    labelKey: 'nav.group.governance',
    items: [
      {
        id: 'numbering',
        labelKey: 'nav.numbering',
        to: ROUTES.NUMBERING,
        icon: Hash,
        permission: Permissions.NumberingView,
        available: true,
      },
      {
        id: 'document-schemas',
        labelKey: 'nav.document-schemas',
        to: ROUTES.DOCUMENT_SCHEMAS,
        icon: ListPlus,
        permission: Permissions.LookupManage,
        available: true,
      },
      {
        id: 'lookups',
        labelKey: 'nav.lookups',
        to: ROUTES.LOOKUPS,
        icon: Library,
        permission: Permissions.LookupManage,
        available: true,
      },
      {
        id: 'audit',
        labelKey: 'nav.audit',
        to: ROUTES.AUDIT,
        icon: ScrollText,
        permission: Permissions.AuditView,
        available: true,
      },
      {
        id: 'system-errors',
        labelKey: 'nav.system-errors',
        to: ROUTES.SYSTEM_ERRORS,
        icon: Bug,
        permission: Permissions.SystemErrorsView,
        available: true,
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