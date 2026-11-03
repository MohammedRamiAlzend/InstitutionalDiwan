import { useState } from 'react'
import { toast } from 'sonner'
import {
  DynamicFieldInputs,
  toFieldValueMap,
  toFieldValueRequests,
  validateFieldValues,
  type DocumentFieldsResponse,
  type FieldValueErrors,
  type FieldValueMap,
} from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { Button } from '@/shared/ui'
import { useSetDocumentFieldValuesMutation } from '../api/fieldValueMutations'

interface EditFieldValuesFormProps {
  documentId: string
  fields: DocumentFieldsResponse
  onDone: () => void
}

/** Edits the values of a draft against its pinned schema; local checks run before the request. */
export function EditFieldValuesForm({ documentId, fields, onDone }: EditFieldValuesFormProps) {
  const mutation = useSetDocumentFieldValuesMutation(documentId)
  const schemaFields = fields.fields.map((entry) => entry.field)
  const [values, setValues] = useState<FieldValueMap>(() => toFieldValueMap(fields.fields))
  const [errors, setErrors] = useState<FieldValueErrors>({})

  const submit = () => {
    const found = validateFieldValues(schemaFields, values)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    mutation.mutate(toFieldValueRequests(schemaFields, values), {
      onSuccess: () => {
        toast.success('حُفظت قيم الحقول.')
        onDone()
      },
    })
  }

  return (
    <form
      noValidate
      className="flex max-w-2xl flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault()
        submit()
      }}
    >
      <DynamicFieldInputs
        fields={schemaFields}
        values={values}
        errors={errors}
        disabled={mutation.isPending}
        onChange={(key, value) => {
          setValues((current) => ({ ...current, [key]: value }))
          setErrors((current) =>
            key in current
              ? Object.fromEntries(Object.entries(current).filter(([k]) => k !== key))
              : current,
          )
        }}
      />

      {mutation.error && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {toArabicErrorMessage(mutation.error)}
        </p>
      )}

      <div className="flex items-center gap-2">
        <Button type="submit" loading={mutation.isPending}>
          حفظ القيم
        </Button>
        <Button type="button" variant="outline" onClick={onDone} disabled={mutation.isPending}>
          تراجع
        </Button>
      </div>
    </form>
  )
}
