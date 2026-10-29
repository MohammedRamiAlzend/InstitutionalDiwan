import { useState } from 'react'
import { Building2, GitBranch, RefreshCw, Route } from 'lucide-react'
import {
  currentMovementStep,
  getMovementActions,
  hasActiveMovement,
  isMovementTerminal,
  MovementStatusBadge,
  MovementStepStatusBadge,
  MovementTypeBadge,
  translateMovementNote,
  useDocumentMovements,
  type MovementAction,
  type MovementResponse,
  type MovementStepResponse,
} from '@/entities/movement'
import { DOCUMENT_STATUS, LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import {
  MovementActionButtons,
  MovementActionDialog,
  OpenMovementDialog,
} from '@/features/movement-routing'
import { toArabicErrorMessage } from '@/shared/api'
import { MOVEMENT_STEP_STATUS } from '@/shared/config'
import { formatDate, formatDateTime, formatShortId } from '@/shared/lib'
import { Button, Skeleton } from '@/shared/ui'

interface DocumentMovementsTabProps {
  documentId: string
  documentTitle: string
  documentStatusId: number
  canEdit: boolean
  departmentNames: Map<string, string>
}

/** Movements tab: routing timeline, step history, and the actions the status allows. */
export function DocumentMovementsTab({
  documentId,
  documentTitle,
  documentStatusId,
  canEdit,
  departmentNames,
}: DocumentMovementsTabProps) {
  const movementsQuery = useDocumentMovements(documentId)

  const [openMovementOpen, setOpenMovementOpen] = useState(false)
  const [pendingAction, setPendingAction] = useState<{
    action: MovementAction
    movementId: string
  } | null>(null)

  const movements = movementsQuery.data ?? []
  const isRegistered = documentStatusId === DOCUMENT_STATUS.Registered
  const canOpenMovement =
    canEdit && isRegistered && movementsQuery.isSuccess && !hasActiveMovement(movements)

  const departmentNameOf = (departmentId: string) =>
    departmentNames.get(departmentId) ?? formatShortId(departmentId)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-start justify-between gap-3 rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-col gap-1">
          <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
            <Route className="size-4 text-accent" aria-hidden />
            حركات الوثيقة
          </h2>
          <p className="max-w-2xl text-xs text-muted">
            كل حركة تفتح مسارًا بين قسمين يمر عبر الديوان المركزي. الاستلام يتطلب تسجيل القسم
            لرقمه أولًا، والرفض يعيد الوثيقة إلى الديوان.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {canOpenMovement && (
            <Button type="button" size="sm" onClick={() => setOpenMovementOpen(true)}>
              <GitBranch className="size-4" aria-hidden />
              فتح حركة
            </Button>
          )}
          <Button
            type="button"
            size="icon"
            variant="outline"
            className="size-9"
            onClick={() => void movementsQuery.refetch()}
            aria-label="تحديث الحركات"
          >
            <RefreshCw
              className={movementsQuery.isFetching ? 'size-4 animate-spin' : 'size-4'}
              aria-hidden
            />
          </Button>
        </div>
      </section>

      {!isRegistered && movementsQuery.isSuccess && movements.length === 0 && (
        <p className="rounded-lg border border-dashed border-border px-4 py-3 text-xs text-muted">
          لا تُفتح الحركات إلا للوثائق المسجَّلة. سجّل الوثيقة برقم رسمي أولًا.
        </p>
      )}

      {isRegistered && movementsQuery.isSuccess && hasActiveMovement(movements) && (
        <p className="rounded-lg border border-accent/40 bg-accent/10 px-4 py-3 text-xs text-foreground">
          توجد حركة مفتوحة على هذه الوثيقة. تُغلق الحركة أو تُلغى قبل فتح حركة جديدة.
        </p>
      )}

      {movementsQuery.isLoading ? (
        <>
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-24 w-full" />
        </>
      ) : movementsQuery.isError ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{toArabicErrorMessage(movementsQuery.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void movementsQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : movements.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted">
          {isRegistered
            ? 'لا توجد حركات لهذه الوثيقة بعد. افتح حركة لتوجيهها إلى قسم آخر عبر الديوان المركزي.'
            : 'ستظهر حركات الوثيقة هنا بعد تسجيلها وفتح أول حركة.'}
        </p>
      ) : (
        movements.map((movement) => (
          <MovementCard
            key={movement.movementId}
            movement={movement}
            canEdit={canEdit}
            departmentNameOf={departmentNameOf}
            onAction={(action) =>
              setPendingAction({ action, movementId: movement.movementId })
            }
          />
        ))
      )}

      {openMovementOpen && (
        <OpenMovementDialog
          documentId={documentId}
          documentTitle={documentTitle}
          open
          onOpenChange={setOpenMovementOpen}
        />
      )}

      {pendingAction && (
        <MovementActionDialog
          action={pendingAction.action}
          movementId={pendingAction.movementId}
          open
          onOpenChange={(open) => {
            if (!open) setPendingAction(null)
          }}
        />
      )}
    </div>
  )
}

interface MovementCardProps {
  movement: MovementResponse
  canEdit: boolean
  departmentNameOf: (departmentId: string) => string
  onAction: (action: MovementAction) => void
}

function MovementCard({
  movement,
  canEdit,
  departmentNameOf,
  onAction,
}: MovementCardProps) {
  const step = currentMovementStep(movement)
  const actions = getMovementActions(movement.statusId, step?.stepStatusId ?? 0, canEdit)
  const terminal = isMovementTerminal(movement.statusId)

  return (
    <section className="flex flex-col gap-5 rounded-xl border border-border bg-surface p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <MovementTypeBadge movementTypeId={movement.movementTypeId} />
          <MovementStatusBadge statusId={movement.statusId} />
          {!terminal && (
            <span className="text-xs text-muted">
              الخطوة الحالية <span dir="ltr">{movement.currentStepNo}</span>
            </span>
          )}
          {movement.dueDate && (
            <span className="text-xs text-muted">
              الاستحقاق: <span dir="ltr">{formatDate(movement.dueDate)}</span>
            </span>
          )}
        </div>
        <span className="text-xs text-muted">
          أُنشئت <span dir="ltr">{formatDateTime(movement.createdAt)}</span>
          {movement.closedAt && (
            <>
              {' · '}
              أُغلقت <span dir="ltr">{formatDateTime(movement.closedAt)}</span>
            </>
          )}
        </span>
      </div>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted">
        <span className="inline-flex items-center gap-1.5">
          <Building2 className="size-3.5" aria-hidden />
          المصدر: <span className="text-foreground">{departmentNameOf(movement.sourceDepartmentId)}</span>
        </span>
        <span className="text-muted">
          الوجهة: <span className="text-foreground">{departmentNameOf(movement.destinationDepartmentId)}</span>
        </span>
        {movement.assignedToUserId && (
          <span>
            المكلَّف: <span dir="ltr">{formatShortId(movement.assignedToUserId)}</span>
          </span>
        )}
        <span>
          أنشأها: <span dir="ltr">{formatShortId(movement.createdByUserId)}</span>
        </span>
      </div>

      {movement.instructions && (
        <p className="rounded-lg bg-surface-muted px-3 py-2 text-xs text-foreground">
          {movement.instructions}
        </p>
      )}

      {actions.length > 0 && (
        <MovementActionButtons actions={actions} onAction={onAction} />
      )}

      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-foreground">مسارات الحركة</h3>
        <ol className="flex flex-col">
          {movement.steps.map((movementStep, index) => (
            <StepTimelineItem
              key={movementStep.stepId}
              step={movementStep}
              isCurrent={!terminal && movementStep.stepNo === movement.currentStepNo}
              isLast={index === movement.steps.length - 1}
              departmentNameOf={departmentNameOf}
            />
          ))}
        </ol>
      </div>

      {movement.events.length > 0 && (
        <div className="flex flex-col gap-2">
          <h3 className="text-sm font-semibold text-foreground">سجل الأحداث</h3>
          <ul className="flex flex-col gap-1.5">
            {movement.events.map((event) => (
              <li
                key={event.eventId}
                className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-lg bg-surface-muted/60 px-3 py-1.5 text-xs"
              >
                <LookupLabel
                  lookup={LOOKUP_SLUGS.MovementEventTypes}
                  id={event.eventTypeId}
                  className="text-xs font-medium"
                />
                {event.stepNo !== null && (
                  <span className="text-muted">
                    خطوة <span dir="ltr">{event.stepNo}</span>
                  </span>
                )}
                <span className="text-muted" dir="ltr">
                  {formatDateTime(event.occurredAt)}
                </span>
                <span className="text-muted">
                  بواسطة <span dir="ltr">{formatShortId(event.actorUserId)}</span>
                </span>
                {translateMovementNote(event.notes) && (
                  <span className="text-foreground">{translateMovementNote(event.notes)}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}

interface StepTimelineItemProps {
  step: MovementStepResponse
  isCurrent: boolean
  isLast: boolean
  departmentNameOf: (departmentId: string) => string
}

/** The API stamps `completedAt` on any step end, so the label follows the step outcome. */
function stepEndLabel(stepStatusId: number): string {
  switch (stepStatusId) {
    case MOVEMENT_STEP_STATUS.Completed:
      return 'اكتملت'
    case MOVEMENT_STEP_STATUS.Rejected:
      return 'رُفضت'
    case MOVEMENT_STEP_STATUS.Returned:
      return 'أُعيدت'
    case MOVEMENT_STEP_STATUS.Redirected:
      return 'أُعيد توجيهها'
    default:
      return 'أُنهيت'
  }
}

function StepTimelineItem({
  step,
  isCurrent,
  isLast,
  departmentNameOf,
}: StepTimelineItemProps) {
  return (
    <li className="flex gap-3">
      <div className="flex flex-col items-center">
        <span
          className={
            isCurrent
              ? 'flex size-7 shrink-0 items-center justify-center rounded-full bg-accent/15 text-xs font-semibold text-accent'
              : 'flex size-7 shrink-0 items-center justify-center rounded-full bg-surface-muted text-xs font-semibold text-muted'
          }
        >
          {step.stepNo}
        </span>
        {!isLast && <span className="mt-1 w-px flex-1 bg-border" aria-hidden />}
      </div>

      <div className="flex flex-1 flex-col gap-2 pb-5">
        <div className="flex flex-wrap items-center gap-2">
          <MovementStepStatusBadge stepStatusId={step.stepStatusId} />
          <span className="text-xs text-muted">
            {departmentNameOf(step.fromDepartmentId)} ← {departmentNameOf(step.toDepartmentId)}
          </span>
          {isCurrent && <span className="text-[10px] text-accent">الخطوة الحالية</span>}
        </div>

        <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
          {step.sentAt && (
            <span>
              أُرسلت <span dir="ltr">{formatDateTime(step.sentAt)}</span>
            </span>
          )}
          {step.receivedAt && (
            <span>
              استُلمت <span dir="ltr">{formatDateTime(step.receivedAt)}</span>
            </span>
          )}
          {step.completedAt && (
            <span>
              {stepEndLabel(step.stepStatusId)}{' '}
              <span dir="ltr">{formatDateTime(step.completedAt)}</span>
            </span>
          )}
          {step.registryNumberId && (
            <span>
              رقم الدائرة: <span dir="ltr">{formatShortId(step.registryNumberId)}</span>
            </span>
          )}
          {step.dueDate && (
            <span>
              الاستحقاق <span dir="ltr">{formatDate(step.dueDate)}</span>
            </span>
          )}
          {step.assignedToUserId && (
            <span>
              المكلَّف <span dir="ltr">{formatShortId(step.assignedToUserId)}</span>
            </span>
          )}
        </div>

        {translateMovementNote(step.instructions) && (
          <p className="text-xs text-foreground">{translateMovementNote(step.instructions)}</p>
        )}
        {translateMovementNote(step.outcomeNotes) && (
          <p className="rounded-md bg-surface-muted px-2.5 py-1.5 text-xs text-foreground">
            {translateMovementNote(step.outcomeNotes)}
          </p>
        )}
      </div>
    </li>
  )
}
