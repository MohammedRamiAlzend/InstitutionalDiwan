import { useState } from 'react'
import { toast } from 'sonner'
import {
  DynamicFieldInputs,
  toFieldValueMap,
  toFieldValueRequests,
  validateFieldValues,
  withFixedValues,
  type DepartmentRecordVisitResponse,
  type FieldValueErrors,
  type FieldValueMap,
} from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, FormField, Textarea } from '@/shared/ui'
import { useCorrectVisitRecordMutation, useSetVisitRecordMutation } from '../api/recordMutations'

/** Matches `DocumentRegistryVisit.MaxCorrectionReasonLength` on the server. */
const MAX_REASON_LENGTH = 1000

interface EditVisitRecordFormProps {
  documentId: string
  visit: DepartmentRecordVisitResponse
  /** `edit` changes an open record; `correct` changes a locked one and asks for a reason. */
  mode: 'edit' | 'correct'
  onDone: () => void
}

/**
 * Edits the record a department keeps on one visit, against the form version the visit was opened
 * with. Required fields must stay filled. A correction of a locked record also needs a reason, which
 * is kept on the visit and in the audit trail.
 */
export function EditVisitRecordForm({ documentId, visit, mode, onDone }: EditVisitRecordFormProps) {
  const setMutation = useSetVisitRecordMutation(documentId)
  const correctMutation = useCorrectVisitRecordMutation(documentId)
  const mutation = mode === 'edit' ? setMutation : correctMutation
  const schemaFields = visit.fields.map((entry) => entry.field)
  const [values, setValues] = useState<FieldValueMap>(() =>
    withFixedValues(schemaFields, toFieldValueMap(visit.fields)),
  )
  const [errors, setErrors] = useState<FieldValueErrors>({})
  const [reason, setReason] = useState('')
  const [reasonError, setReasonError] = useState<string>()

  const submit = () => {
    const found = validateFieldValues(schemaFields, values)
    setErrors(found)
    const trimmedReason = reason.trim()
    const reasonProblem =
      mode !== 'correct'
        ? undefined
        : !trimmedReason
          ? 'سبب التصحيح مطلوب.'
          : trimmedReason.length > MAX_REASON_LENGTH
            ? 'سبب التصحيح يتجاوز 1000 حرف.'
            : undefined
    setReasonError(reasonProblem)
    if (Object.keys(found).length > 0 || reasonProblem) return

    const requests = toFieldValueRequests(schemaFields, values)
    const onSuccess = () => {
      toast.success(mode === 'edit' ? 'حُفظ سجل الديوان.' : 'صُحّح السجل المقفل.')
      onDone()
    }
    if (mode === 'edit') {
      setMutation.mutate({ visitId: visit.visitId, values: requests }, { onSuccess })
    } else {
      correctMutation.mutate({ visitId: visit.visitId, values: requests, reason: trimmedReason }, { onSuccess })
    }
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

      {mode === 'correct' && (
        <FormField
          label="سبب التصحيح"
          required
          error={reasonError}
          htmlFor={`record-correct-reason-${visit.visitId}`}
        >
          <Textarea
            id={`record-correct-reason-${visit.visitId}`}
            rows={2}
            maxLength={MAX_REASON_LENGTH}
            placeholder="لماذا يُصحَّح سجل أُقفل بخروج الوثيقة؟"
            invalid={Boolean(reasonError)}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </FormField>
      )}

      {mutation.error && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {toArabicErrorMessage(mutation.error)}
        </p>
      )}

      <div className="flex items-center gap-2">
        <Button type="submit" loading={mutation.isPending}>
          {mode === 'edit' ? 'حفظ السجل' : 'حفظ التصحيح'}
        </Button>
        <Button type="button" variant="outline" onClick={onDone} disabled={mutation.isPending}>
          تراجع
        </Button>
      </div>
    </form>
  )
}
