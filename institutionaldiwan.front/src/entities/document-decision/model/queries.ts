import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { documentDecisionApi } from '../api/documentDecisionApi'
import type {
  DecisionRegisterItemResponse,
  DecisionRegisterParams,
  DocumentDecisionResponse,
  JoinCandidateResponse,
  NextActionResponse,
} from './types'

export const documentDecisionKeys = {
  all: ['document-decisions'] as const,
  forDocument: (documentId: string) => [...documentDecisionKeys.all, documentId] as const,
  list: (documentId: string) => [...documentDecisionKeys.forDocument(documentId), 'list'] as const,
  next: (documentId: string) => [...documentDecisionKeys.forDocument(documentId), 'next'] as const,
  candidates: (documentId: string) =>
    [...documentDecisionKeys.forDocument(documentId), 'join-candidates'] as const,
  register: (params: DecisionRegisterParams) =>
    [...documentDecisionKeys.all, 'register', params] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

export function useDocumentDecisions(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentDecisionResponse[]>,
) {
  return useQuery({
    queryKey: documentDecisionKeys.list(documentId ?? ''),
    queryFn: () => documentDecisionApi.listByDocument(documentId as string),
    staleTime: 15 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

export function useDocumentNextAction(
  documentId: string | null | undefined,
  options?: EnabledOptions<NextActionResponse>,
) {
  return useQuery({
    queryKey: documentDecisionKeys.next(documentId ?? ''),
    queryFn: () => documentDecisionApi.nextAction(documentId as string),
    staleTime: 15 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

export function useJoinCandidates(
  documentId: string | null | undefined,
  options?: EnabledOptions<JoinCandidateResponse[]>,
) {
  return useQuery({
    queryKey: documentDecisionKeys.candidates(documentId ?? ''),
    queryFn: () => documentDecisionApi.joinCandidates(documentId as string),
    staleTime: 15 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

export function useDecisionRegister(params: DecisionRegisterParams) {
  return useQuery({
    queryKey: documentDecisionKeys.register(params),
    queryFn: () => documentDecisionApi.register(params),
    placeholderData: (previous) => previous,
    staleTime: 15 * 1000,
  })
}

export type DecisionRegisterPage = PagedResult<DecisionRegisterItemResponse>
