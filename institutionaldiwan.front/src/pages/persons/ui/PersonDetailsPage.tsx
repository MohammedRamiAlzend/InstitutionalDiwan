import { Link, Navigate, useParams, useSearchParams } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'
import { usePerson } from '@/entities/person'
import { RequirePermission } from '@/features/auth'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { GuideProvider, GuideRegion, PageHeader, Skeleton } from '@/shared/ui'
import { PersonDocumentsTable } from '@/widgets/person-documents-table'
import { PersonTimeline } from '@/widgets/person-timeline'

const TABS = [
  { key: 'documents', label: 'الوثائق' },
  { key: 'timeline', label: 'السجل الزمني' },
] as const

/** `/persons/:personId` — a person's documents and everything that happened to them. */
export function PersonDetailsPage() {
  return (
    <RequirePermission permission={Permissions.PersonView}>
      <GuideProvider pageId="person-details">
        <PersonDetailsView />
      </GuideProvider>
    </RequirePermission>
  )
}

function PersonDetailsView() {
  const { personId } = useParams<{ personId: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const personQuery = usePerson(personId)
  const activeTab = searchParams.get('tab') === 'timeline' ? 'timeline' : 'documents'

  if (!personId) return <Navigate to={ROUTES.PERSONS} replace />

  if (personQuery.isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-10 w-1/2" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (personQuery.isError || !personQuery.data) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-12 text-center">
        <p className="text-sm font-medium text-danger">{toArabicErrorMessage(personQuery.error)}</p>
        <Link to={ROUTES.PERSONS} className="text-sm text-accent hover:underline">
          العودة إلى الأشخاص
        </Link>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        to={ROUTES.PERSONS}
        className="inline-flex w-fit items-center gap-1 text-sm text-muted transition-colors hover:text-foreground"
      >
        <ArrowRight className="size-4" aria-hidden />
        العودة إلى الأشخاص
      </Link>

      <PageHeader title={personQuery.data.displayName} pageGuide="person-details" />

      <nav aria-label="تبويبات ملف الشخص" className="flex items-center gap-1 border-b border-border pb-1">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            aria-current={activeTab === tab.key ? 'page' : undefined}
            onClick={() => setSearchParams({ tab: tab.key })}
            className={cn(
              'rounded-t-md px-3 py-2 text-sm transition-colors',
              activeTab === tab.key
                ? 'border-b-2 border-accent font-semibold text-accent'
                : 'text-muted hover:bg-surface-muted hover:text-foreground',
            )}
          >
            {tab.label}
          </button>
        ))}
      </nav>

      {activeTab === 'documents' ? (
        <GuideRegion id="person-details.documents">
          <PersonDocumentsTable personId={personId} />
        </GuideRegion>
      ) : (
        <GuideRegion id="person-details.timeline">
          <PersonTimeline personId={personId} />
        </GuideRegion>
      )}
    </div>
  )
}
