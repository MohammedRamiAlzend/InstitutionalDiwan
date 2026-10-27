import axios from 'axios'
import { ENV } from '@/shared/config'
import { tokenStorage } from './tokenStorage'
import { ApiError, toApiError } from './problemDetails'

/**
 * The single Axios instance for all API calls. Attaches the bearer token,
 * and normalizes every failure to `ApiError`. A `401` clears the session and
 * sends the user back to `/login`.
 */
export const httpClient = axios.create({
  baseURL: ENV.API_BASE_URL,
})

httpClient.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const apiError = toApiError(error)

    if (apiError.status === 401 && !window.location.pathname.startsWith('/login')) {
      tokenStorage.clear()
      window.location.assign('/login')
    }

    return Promise.reject(apiError)
  },
)

export { ApiError }
