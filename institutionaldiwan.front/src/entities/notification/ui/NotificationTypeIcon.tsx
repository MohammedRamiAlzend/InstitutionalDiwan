import type { LucideIcon } from 'lucide-react'
import {
  Archive,
  ArrowLeftRight,
  Bell,
  CheckCheck,
  FileCheck2,
  FilePlus2,
  Inbox,
  Lock,
  Send,
  Undo2,
  XCircle,
} from 'lucide-react'
import { NOTIFICATION_TYPE } from '@/entities/lookup'

const ICONS: Record<number, LucideIcon> = {
  [NOTIFICATION_TYPE.DocumentCreated]: FilePlus2,
  [NOTIFICATION_TYPE.DocumentRegistered]: FileCheck2,
  [NOTIFICATION_TYPE.MovementSent]: Send,
  [NOTIFICATION_TYPE.MovementReceived]: Inbox,
  [NOTIFICATION_TYPE.MovementCompleted]: CheckCheck,
  [NOTIFICATION_TYPE.DocumentArchived]: Archive,
  [NOTIFICATION_TYPE.MovementRejected]: XCircle,
  [NOTIFICATION_TYPE.MovementReturned]: Undo2,
  [NOTIFICATION_TYPE.MovementRedirected]: ArrowLeftRight,
  [NOTIFICATION_TYPE.MovementClosed]: Lock,
  [NOTIFICATION_TYPE.MovementCancelled]: XCircle,
}

/** Decorative icon for a notification type; the type name is always rendered beside it as text. */
export function NotificationTypeIcon({ typeId, className }: { typeId: number; className?: string }) {
  const Icon = ICONS[typeId] ?? Bell
  return <Icon className={className ?? 'size-4'} aria-hidden />
}
