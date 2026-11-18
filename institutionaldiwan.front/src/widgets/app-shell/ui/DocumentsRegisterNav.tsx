import { BookMarked } from 'lucide-react'
import { useDepartmentRegistries } from '@/entities/numbering-registry'
import { useMyPrimaryDepartmentId, useSessionStore } from '@/entities/user'
import { Permissions } from '@/shared/config'
import {
  cn,
  resolveSelectedRegistryId,
  t,
  useSelectedRegistryId,
  writeSelectedRegistryId,
} from '@/shared/lib'

/**
 * The caller's own registers under «الوثائق». Picking one only stores the choice that
 * preselects the register in the create-document form; it never navigates.
 */
export function DocumentsRegisterNav() {
  const canViewNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingView),
  )
  const { departmentId } = useMyPrimaryDepartmentId()
  const { registries, isLoading } = useDepartmentRegistries(departmentId, {
    enabled: canViewNumbering,
  })
  const stored = useSelectedRegistryId()
  const selectedId = resolveSelectedRegistryId(
    stored,
    registries.map((registry) => ({ value: registry.registryId })),
  )

  if (!canViewNumbering || isLoading || registries.length === 0) return null

  return (
    <ul
      aria-label={t('shell.register-nav-aria')}
      className="ms-5 mt-0.5 flex flex-col gap-0.5 border-s border-border ps-2"
    >
      {registries.map((registry) => {
        const selected = registry.registryId === selectedId
        return (
          <li key={registry.registryId}>
            <button
              type="button"
              aria-pressed={selected}
              title={`${registry.code} — ${registry.nameAr}`}
              onClick={() => writeSelectedRegistryId(registry.registryId)}
              className={cn(
                'flex w-full cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-xs transition-colors',
                selected
                  ? 'bg-accent/10 font-medium text-accent'
                  : 'text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <BookMarked className="size-3.5 shrink-0" aria-hidden />
              <span className="truncate">{registry.nameAr}</span>
            </button>
          </li>
        )
      })}
    </ul>
  )
}