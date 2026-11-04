import { z } from 'zod'

/** Mirrors `CreateLookupEntryCommandValidator`: positive id, both labels required, ≤ 150 chars. */
export const lookupEntryFormSchema = z.object({
  id: z
    .string()
    .trim()
    .min(1, 'المعرّف مطلوب.')
    .regex(/^[0-9]+$/, 'المعرّف يجب أن يكون عددًا صحيحًا موجبًا.')
    .refine(
      (value) => Number(value) > 0 && Number(value) <= 2147483647,
      'المعرّف يجب أن يكون عددًا صحيحًا موجبًا.',
    ),
  descAr: z
    .string()
    .trim()
    .min(1, 'التسمية العربية مطلوبة.')
    .max(150, 'التسمية العربية تتجاوز 150 حرفًا.'),
  desc: z
    .string()
    .trim()
    .min(1, 'التسمية الإنجليزية مطلوبة.')
    .max(150, 'التسمية الإنجليزية تتجاوز 150 حرفًا.'),
})

export type LookupEntryFormValues = z.infer<typeof lookupEntryFormSchema>
