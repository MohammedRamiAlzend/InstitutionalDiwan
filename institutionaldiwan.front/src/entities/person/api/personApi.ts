import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  DocumentPersonResponse,
  PersonDocumentListParams,
  PersonDocumentResponse,
  PersonListItemResponse,
  PersonListParams,
  PersonResponse,
  PersonSuggestionResponse,
  PersonTimelineEntryResponse,
  PersonTimelineParams,
} from '../model/types'

/** Typed read calls for the people named on documents. */
export const personApi = {
  suggest: async (query: string): Promise<PersonSuggestionResponse[]> => {
    const { data } = await httpClient.get<PersonSuggestionResponse[]>('/persons/suggestions', {
      params: { q: query },
    })
    return data
  },

  list: async (params: PersonListParams): Promise<PagedResult<PersonListItemResponse>> => {
    const { data } = await httpClient.get<PagedResult<PersonListItemResponse>>('/persons', {
      params: { ...params, search: params.search?.trim() || undefined },
    })
    return data
  },

  byId: async (personId: string): Promise<PersonResponse> => {
    const { data } = await httpClient.get<PersonResponse>(`/persons/${personId}`)
    return data
  },

  documents: async (
    personId: string,
    params: PersonDocumentListParams,
  ): Promise<PagedResult<PersonDocumentResponse>> => {
    const { data } = await httpClient.get<PagedResult<PersonDocumentResponse>>(
      `/persons/${personId}/documents`,
      { params: { ...params, search: params.search?.trim() || undefined } },
    )
    return data
  },

  timeline: async (
    personId: string,
    params: PersonTimelineParams,
  ): Promise<PagedResult<PersonTimelineEntryResponse>> => {
    const { data } = await httpClient.get<PagedResult<PersonTimelineEntryResponse>>(
      `/persons/${personId}/timeline`,
      { params },
    )
    return data
  },

  forDocument: async (documentId: string): Promise<DocumentPersonResponse[]> => {
    const { data } = await httpClient.get<DocumentPersonResponse[]>(`/documents/${documentId}/persons`)
    return data
  },
}
