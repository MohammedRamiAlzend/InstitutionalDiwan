import { useSearchParams } from 'react-router-dom'
import { RequirePermission } from '@/features/auth'
import { Permissions, lookupCatalogMeta } from '@/shared/config'
import { GuideChip, GuideProvider, PageHeader } from '@/shared/ui'
import { LookupCatalogPicker, LookupEntriesTable } from '@/widgets/lookup-admin'

/** `/lookups` — reference-catalog administration; the chosen catalog lives in `?catalog=`. */
export function LookupsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const catalog = searchParams.get('catalog')

  // Switching catalogs resets page, search, sort and filters of the previous one.
  const select = (slug: string) => setSearchParams({ catalog: slug })

  return (
    <RequirePermission permission={Permissions.LookupManage}>
      <GuideProvider pageId="lookups">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="الجداول المرجعية"
            description="القيم التي تظهر في القوائم المنسدلة عبر النظام. أي تغيير يسري فورًا على بقية الشاشات."
            pageGuide="lookups"
          />
          <div className="grid gap-6 lg:grid-cols-[18rem_1fr]">
            <LookupCatalogPicker selected={catalog} onSelect={select} />
            <div className="min-w-0">
              {catalog ? (
                <>
                  <h2 className="mb-3 flex items-center gap-2 text-base font-semibold text-foreground">
                    {lookupCatalogMeta(catalog).labelAr}
                    <GuideChip id="lookups.grid" />
                  </h2>
                  <LookupEntriesTable key={catalog} lookup={catalog} />
                </>
              ) : (
                <p className="rounded-xl border border-dashed border-border p-8 text-center text-sm text-muted">
                  اختر جدولًا مرجعيًا من القائمة لعرض قيمه وإدارتها.
                </p>
              )}
            </div>
          </div>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
