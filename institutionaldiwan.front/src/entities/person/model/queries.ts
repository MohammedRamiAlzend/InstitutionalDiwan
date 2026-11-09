import { useQuery } from '@tanstack/react-query'
import { personApi } from '../api/personApi'
import type { PersonDocumentListParams, PersonListParams, PersonTimelineParams } from './types'

const MIN_SUGGESTION_LENGTH = 2

export const personKeys = {
  all: ['persons'] as const,
  suggestions: (query: string) => [...personKeys.all, 'suggestions', query] as const,
  list: (params: PersonListParams) => [...personKeys.all, 'list', params] as const,
  detail: (personId: string) => [...personKeys.all, 'detail', personId] as const,
  documents: (personId: string, params: PersonDocumentListParams) =>
    [...personKeys.detail(personId), 'documents', params] as const,
  timeline: (personId: string, params: PersonTimelineParams) =>
    [...personKeys.detail(personId), 'timeline', params] as const,
  forDocument: (documentId: string) => [...personKeys.all, 'document', documentId] as const,
}

/** Existing people matching what is being typed; silent until two characters are entered. */
export function usePersonSuggestions(query: string) {
  const trimmed = query.trim()
  return useQuery({
    queryKey: personKeys.suggestions(trimmed),
    queryFn: () => personApi.suggest(trimmed),
    enabled: trimmed.length >= MIN_SUGGESTION_LENGTH,
    staleTime: 30 * 1000,
    retry: false,
  })
}

export function usePersons(params: PersonListParams) {
  return useQuery({
    queryKey: personKeys.list(params),
    queryFn: () => personApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  })
}

export function usePerson(personId: string | undefined) {
  return useQuery({
    queryKey: personKeys.detail(personId ?? ''),
    queryFn: () => personApi.byId(personId as string),
    enabled: Boolean(personId),
    staleTime: 30 * 1000,
    retry: false,
  })
}

export function usePersonDocuments(personId: string, params: PersonDocumentListParams) {
  return useQuery({
    queryKey: personKeys.documents(personId, params),
    queryFn: () => personApi.documents(personId, params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  })
}

export function usePersonTimeline(personId: string, params: PersonTimelineParams) {
  return useQuery({
    queryKey: personKeys.timeline(personId, params),
    queryFn: () => personApi.timeline(personId, params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  })
}

export function useDocumentPersons(documentId: string) {
  return useQuery({
    queryKey: personKeys.forDocument(documentId),
    queryFn: () => personApi.forDocument(documentId),
    staleTime: 30 * 1000,
  })
}
