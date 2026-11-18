import type { LabelKey } from '@/shared/config'
import {
  SYSTEM_ERROR_KIND,
  SYSTEM_ERROR_SOURCE,
  SYSTEM_ERROR_STATUS,
  type SystemErrorKind,
  type SystemErrorSource,
  type SystemErrorStatus,
} from './types'

export const SYSTEM_ERROR_STATUS_LABEL_KEYS: Record<SystemErrorStatus, LabelKey> = {
  [SYSTEM_ERROR_STATUS.New]: 'system-error.status.new',
  [SYSTEM_ERROR_STATUS.InProgress]: 'system-error.status.in-progress',
  [SYSTEM_ERROR_STATUS.Resolved]: 'system-error.status.resolved',
}

export const SYSTEM_ERROR_SOURCE_LABEL_KEYS: Record<SystemErrorSource, LabelKey> = {
  [SYSTEM_ERROR_SOURCE.Server]: 'system-error.source.server',
  [SYSTEM_ERROR_SOURCE.Client]: 'system-error.source.client',
}

export const SYSTEM_ERROR_KIND_LABEL_KEYS: Record<SystemErrorKind, LabelKey> = {
  [SYSTEM_ERROR_KIND.ServerException]: 'system-error.kind.server-exception',
  [SYSTEM_ERROR_KIND.ServerRejection]: 'system-error.kind.server-rejection',
  [SYSTEM_ERROR_KIND.ClientRender]: 'system-error.kind.client-render',
  [SYSTEM_ERROR_KIND.ClientScript]: 'system-error.kind.client-script',
  [SYSTEM_ERROR_KIND.ClientNetwork]: 'system-error.kind.client-network',
}