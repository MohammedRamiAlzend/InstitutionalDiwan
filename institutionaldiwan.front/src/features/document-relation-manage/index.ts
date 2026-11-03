export type {
  CreateDocumentRelationRequest,
  RemoveDocumentRelationRequest,
} from './model/types'
export type { CreateRelationFormValues, RemoveRelationFormValues } from './model/schemas'
export { useCreateRelationMutation, useRemoveRelationMutation } from './api/relationMutations'
export { CreateRelationDialog } from './ui/CreateRelationDialog'
export { RemoveRelationDialog } from './ui/RemoveRelationDialog'
