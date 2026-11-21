import { z } from 'zod'

export const drivePermissionSchema = z.object({
  principalTypeId: z.string().min(1, 'اختر نوع الجهة.'),
  principalId: z.string().min(1, 'اختر الجهة.'),
  roleId: z.string().min(1, 'اختر الدور.'),
})

export type DrivePermissionFormValues = z.infer<typeof drivePermissionSchema>
