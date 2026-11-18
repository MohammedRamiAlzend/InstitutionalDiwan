import { useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { useLookupCatalogs } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import {
  LOOKUP_GROUP_ORDER,
  LOOKUP_GROUP_OTHER,
  isLookupCatalogEnabled,
  lookupCatalogMeta,
} from '@/shared/config'
import { cn, resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Button, GuideRegion, Input, Label, Skeleton } from '@/shared/ui'

interface LookupCatalogPickerProps {
  selected: string | null
  onSelect: (slug: string) => void
}

/** Catalog list grouped by module, with a quick filter over Arabic and slug names. */
export function LookupCatalogPicker({ selected, onSelect }: LookupCatalogPickerProps) {
  const catalogsQuery = useLookupCatalogs()
  const [filter, setFilter] = useState('')
  const overrides = useLabelsStore((state) => state.overrides)

  const groups = useMemo(() => {
    const needle = filter.trim().toLocaleLowerCase('ar')
    const order = LOOKUP_GROUP_ORDER.map((key) => resolveLabel(key, overrides))
    const otherLabel = resolveLabel(LOOKUP_GROUP_OTHER, overrides)
    const byGroup = new Map<string, { slug: string; labelAr: string }[]>()
    for (const slug of (catalogsQuery.data ?? []).filter(isLookupCatalogEnabled)) {
      const { labelKey, groupKey } = lookupCatalogMeta(slug)
      const labelAr = labelKey ? resolveLabel(labelKey, overrides) : slug
      const group = resolveLabel(groupKey, overrides)
      if (needle && !`${labelAr} ${slug}`.toLocaleLowerCase('ar').includes(needle)) continue
      byGroup.set(group, [...(byGroup.get(group) ?? []), { slug, labelAr }])
    }
    const rank = (group: string) => {
      const index = order.indexOf(group)
      return index === -1 ? order.indexOf(otherLabel) : index
    }
    return [...byGroup.entries()].sort(([a], [b]) => rank(a) - rank(b))
  }, [catalogsQuery.data, filter, overrides])

  return (
    <GuideRegion id="lookups.catalog">
      <nav
        aria-label={t('lookup.picker.aria')}
        className="flex flex-col gap-3 rounded-xl border border-border bg-surface p-3"
      >
        <Input
          aria-label={t('lookup.picker.search-aria')}
          placeholder={t('lookup.picker.search-placeholder')}
          startIcon={<Search className="size-4" aria-hidden />}
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
        />

        {catalogsQuery.isLoading && (
          <div className="flex flex-col gap-2">
            <Skeleton className="h-8" />
            <Skeleton className="h-8" />
            <Skeleton className="h-8" />
          </div>
        )}

        {catalogsQuery.isError && (
          <div role="alert" className="flex flex-col gap-2 text-sm text-danger">
            {toArabicErrorMessage(catalogsQuery.error)}
            <Button type="button" variant="outline" size="sm" onClick={() => void catalogsQuery.refetch()}>
              <Label k="common.action.retry" />
            </Button>
          </div>
        )}

        {catalogsQuery.isSuccess && groups.length === 0 && (
          <p className="text-sm text-muted"><Label k="lookup.picker.empty" /></p>
        )}

        <div className="flex max-h-[60vh] flex-col gap-3 overflow-y-auto">
          {groups.map(([group, catalogs]) => (
            <section key={group} className="flex flex-col gap-1">
              <h3 className="px-2 text-xs font-semibold text-muted">{group}</h3>
              {catalogs.map(({ slug, labelAr }) => (
                <button
                  key={slug}
                  type="button"
                  onClick={() => onSelect(slug)}
                  aria-current={slug === selected ? 'true' : undefined}
                  className={cn(
                    'cursor-pointer rounded-lg px-2 py-1.5 text-start text-sm transition-colors',
                    slug === selected
                      ? 'bg-accent/10 font-semibold text-accent'
                      : 'text-foreground hover:bg-surface-muted',
                  )}
                >
                  {labelAr}
                </button>
              ))}
            </section>
          ))}
        </div>
      </nav>
    </GuideRegion>
  )
}
