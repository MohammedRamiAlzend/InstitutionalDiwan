import { z } from 'zod'
import { t } from '@/shared/lib'

const personEntry = z.object({
  personId: z.string().nullable(),
  fullName: z.string().trim().min(1).max(300, t('persons.add.validation.name-max')),
})

export const addDocumentPersonsSchema = z.object({
  persons: z.array(personEntry).min(1, t('persons.add.validation.min-one')),
  roleId: z.string().min(1, t('persons.validation.role-required')),
})

export type AddDocumentPersonsFormValues = z.infer<typeof addDocumentPersonsSchema>

export const changeDocumentPersonRoleSchema = z.object({
  roleId: z.string().min(1, t('persons.validation.role-required')),
})

export type ChangeDocumentPersonRoleFormValues = z.infer<typeof changeDocumentPersonRoleSchema>
