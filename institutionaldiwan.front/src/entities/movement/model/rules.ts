import { MOVEMENT_STATUS, MOVEMENT_STEP_STATUS } from '@/shared/config'
import type { MovementAction, MovementResponse, MovementStepResponse } from './types'

export function isMovementTerminal(statusId: number): boolean {
  return statusId === MOVEMENT_STATUS.Closed || statusId === MOVEMENT_STATUS.Canceled
}

/** A document can have only one movement that has not been closed or canceled. */
export function hasActiveMovement(movements: readonly { statusId: number }[]): boolean {
  return movements.some((movement) => !isMovementTerminal(movement.statusId))
}

export function currentMovementStep(
  movement: MovementResponse,
): MovementStepResponse | undefined {
  return movement.steps.find((step) => step.stepNo === movement.currentStepNo)
}

/** Which side of the current step the caller's department scope covers. */
export interface MovementCustody {
  sender: boolean
  receiver: boolean
}

export function getMovementCustody(
  step: { fromDepartmentId: string; toDepartmentId: string } | undefined,
  coversDepartment: (departmentId: string) => boolean,
): MovementCustody {
  return {
    sender: step !== undefined && coversDepartment(step.fromDepartmentId),
    receiver: step !== undefined && coversDepartment(step.toDepartmentId),
  }
}

/** Mirrors the API's `MovementCustody` checks: a movement not sent yet is still the sender's. */
function custodyAllows(
  action: MovementAction,
  statusId: number,
  custody: MovementCustody,
): boolean {
  switch (action) {
    case 'send':
    case 'cancel':
      return custody.sender
    case 'redirect':
      return statusId === MOVEMENT_STATUS.Draft ? custody.sender : custody.receiver
    case 'close':
      return custody.sender || custody.receiver
    default:
      return custody.receiver
  }
}

/**
 * The actions the current status allows on the caller's side of the current step. It mirrors the
 * domain transitions and custody rules; the API stays the final judge and answers
 * `Movements.InvalidStatusTransition` or `Movements.DepartmentOutOfScope` when they disagree.
 */
export function getMovementActions(
  statusId: number,
  stepStatusId: number,
  canEdit: boolean,
  custody: MovementCustody,
): MovementAction[] {
  if (!canEdit || isMovementTerminal(statusId)) return []

  return actionsForStatus(statusId, stepStatusId).filter((action) =>
    custodyAllows(action, statusId, custody),
  )
}

function actionsForStatus(statusId: number, stepStatusId: number): MovementAction[] {
  switch (statusId) {
    case MOVEMENT_STATUS.Draft:
      return ['send', 'redirect', 'cancel']
    case MOVEMENT_STATUS.Sent:
      return stepStatusId === MOVEMENT_STEP_STATUS.Sent
        ? ['receive', 'reject', 'redirect', 'close', 'cancel']
        : ['reject', 'redirect', 'close', 'cancel']
    case MOVEMENT_STATUS.Received:
    case MOVEMENT_STATUS.InProgress:
      return ['complete', 'reject', 'return', 'redirect', 'close', 'cancel']
    case MOVEMENT_STATUS.Redirected:
      return ['send', 'close', 'cancel']
    case MOVEMENT_STATUS.Completed:
    case MOVEMENT_STATUS.Rejected:
    case MOVEMENT_STATUS.Returned:
      return ['close']
    default:
      return []
  }
}

/** The API writes a few English system notes; keep the timeline Arabic. */
export function translateMovementNote(note: string | null): string | null {
  if (!note) return null

  const routed = /^Rejected step routed to the Central Diwan:\s*(.+)$/.exec(note)
  if (routed) {
    return `أُعيد توجيه الوثيقة إلى الديوان المركزي بعد الرفض: ${routed[1]}`
  }

  const rejected = /^Rejected:\s*(.+)$/.exec(note)
  if (rejected) return `مرفوض: ${rejected[1]}`

  return note
}
