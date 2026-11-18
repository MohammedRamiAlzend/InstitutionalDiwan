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
import { t } from '@/shared/lib'
import { Button, FormField, Label, Textarea } from '@/shared/ui'
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
          ? t('department.records.reason-required')
          : trimmedReason.length > MAX_REASON_LENGTH
            ? t('department.records.reason-max')
            : undefined
    setReasonError(reasonProblem)
    if (Object.keys(found).length > 0 || reasonProblem) return

    const requests = toFieldValueRequests(schemaFields, values)
    const onSuccess = () => {
      toast.success(mode === 'edit' ? t('department.records.toast-saved') : t('department.records.toast-corrected'))
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
          labelKey="department.records.reason-label"
          required
          error={reasonError}
          htmlFor={`record-correct-reason-${visit.visitId}`}
        >
          <Textarea
            id={`record-correct-reason-${visit.visitId}`}
            rows={2}
            maxLength={MAX_REASON_LENGTH}
            placeholder={t('department.records.reason-placeholder')}
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
          {mode === 'edit' ? <Label k="department.records.save" /> : <Label k="department.records.save-correction" />}
        </Button>
        <Button type="button" variant="outline" onClick={onDone} disabled={mutation.isPending}>
          <Label k="department.records.dismiss" />
        </Button>
      </div>
    </form>
  )
}
