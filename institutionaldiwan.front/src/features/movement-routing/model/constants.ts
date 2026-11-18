import {
  Ban,
  CheckCircle2,
  Forward,
  Inbox,
  Lock,
  Send,
  Undo2,
  XCircle,
  type LucideIcon,
} from 'lucide-react'
import type { MovementAction } from '@/entities/movement'
import type { LabelKey } from '@/shared/config'

export const MOVEMENT_ACTION_LABEL_KEYS: Record<MovementAction, LabelKey> = {
  send: 'movements.action.send',
  receive: 'movements.action.receive',
  complete: 'movements.action.complete',
  reject: 'movements.action.reject',
  return: 'movements.action.return',
  redirect: 'movements.action.redirect',
  close: 'movements.action.close',
  cancel: 'movements.action.cancel',
}

export const MOVEMENT_ACTION_ICONS: Record<MovementAction, LucideIcon> = {
  send: Send,
  receive: Inbox,
  complete: CheckCircle2,
  reject: XCircle,
  return: Undo2,
  redirect: Forward,
  close: Lock,
  cancel: Ban,
}

export const DESTRUCTIVE_ACTIONS: readonly MovementAction[] = ['reject', 'cancel']
