import {
  SYSTEM_ERROR_KIND,
  SYSTEM_ERROR_SOURCE,
  SYSTEM_ERROR_STATUS,
  type SystemErrorKind,
  type SystemErrorSource,
  type SystemErrorStatus,
} from './types'

export const SYSTEM_ERROR_STATUS_LABELS: Record<SystemErrorStatus, string> = {
  [SYSTEM_ERROR_STATUS.New]: 'جديد',
  [SYSTEM_ERROR_STATUS.InProgress]: 'قيد المعالجة',
  [SYSTEM_ERROR_STATUS.Resolved]: 'محلول',
}

export const SYSTEM_ERROR_SOURCE_LABELS: Record<SystemErrorSource, string> = {
  [SYSTEM_ERROR_SOURCE.Server]: 'الخادم',
  [SYSTEM_ERROR_SOURCE.Client]: 'الواجهة',
}

export const SYSTEM_ERROR_KIND_LABELS: Record<SystemErrorKind, string> = {
  [SYSTEM_ERROR_KIND.ServerException]: 'استثناء في الخادم',
  [SYSTEM_ERROR_KIND.ServerRejection]: 'طلب مرفوض',
  [SYSTEM_ERROR_KIND.ClientRender]: 'انهيار صفحة',
  [SYSTEM_ERROR_KIND.ClientScript]: 'خطأ برمجي في المتصفح',
  [SYSTEM_ERROR_KIND.ClientNetwork]: 'فشل الاتصال',
}
