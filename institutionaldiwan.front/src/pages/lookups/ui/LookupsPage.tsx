import { useSearchParams } from 'react-router-dom'
import { RequirePermission } from '@/features/auth'
import { Permissions, lookupCatalogMeta } from '@/shared/config'
import { GuideChip, GuideProvider, Label, PageHeader } from '@/shared/ui'
import { LookupCatalogPicker, LookupEntriesTable } from '@/widgets/lookup-admin'

/** `/lookups` — reference-catalog administration; the chosen catalog lives in `?catalog=`. */
export function LookupsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const catalog = searchParams.get('catalog')
  const catalogMeta = catalog ? lookupCatalogMeta(catalog) : null

  // Switching catalogs resets page, search, sort and filters of the previous one.
  const select = (slug: string) => setSearchParams({ catalog: slug })

  return (
    <RequirePermission permission={Permissions.LookupManage}>
      <GuideProvider pageId="lookups">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.lookups.title"
            descriptionKey="page.lookups.description"
            pageGuide="lookups"
          />
          <div className="grid gap-6 lg:grid-cols-[18rem_1fr]">
            <LookupCatalogPicker selected={catalog} onSelect={select} />
            <div className="min-w-0">
              {catalog && catalogMeta ? (
                <>
                  <h2 className="mb-3 flex items-center gap-2 text-base font-semibold text-foreground">
                    {catalogMeta.labelKey ? <Label k={catalogMeta.labelKey} /> : catalog}
                    <GuideChip id="lookups.grid" />
                  </h2>
                  <LookupEntriesTable key={catalog} lookup={catalog} />
                </>
              ) : (
                <p className="rounded-xl border border-dashed border-border p-8 text-center text-sm text-muted">
                  <Label k="lookup.page.select-prompt" />
                </p>
              )}
            </div>
          </div>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
