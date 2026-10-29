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

export const MOVEMENT_ACTION_LABELS: Record<MovementAction, string> = {
  send: 'إرسال',
  receive: 'استلام',
  complete: 'إكمال',
  reject: 'رفض',
  return: 'إرجاع',
  redirect: 'إعادة توجيه',
  close: 'إغلاق',
  cancel: 'إلغاء الحركة',
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
