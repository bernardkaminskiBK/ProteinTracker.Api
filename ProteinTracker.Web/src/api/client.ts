import type {
  DailySummaryResponse,
  DailyTargetResponse,
  FoodEntryRequest,
  FoodEntryResponse,
  FoodRequest,
  FoodResponse,
  ProblemDetails,
  UpdateDailyTargetRequest,
} from '../types/api'

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL || '/api'
const apiBaseUrl = configuredBaseUrl.replace(/\/$/, '')

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...init?.headers,
      },
    })
  } catch {
    throw new ApiError(
      'Unable to connect. Check that Protein Tracker is running.',
      0,
    )
  }

  if (!response.ok) {
    let problem: ProblemDetails | undefined

    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      problem = undefined
    }

    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(' ')
      : undefined

    throw new ApiError(
      problem?.detail || validationMessage || problem?.title || `Request failed (${response.status}).`,
      response.status,
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const foodsApi = {
  getActive: () => request<FoodResponse[]>('/foods'),
  getArchived: () => request<FoodResponse[]>('/foods/archived'),
  create: (payload: FoodRequest) =>
    request<FoodResponse>('/foods', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  update: (id: number, payload: FoodRequest) =>
    request<FoodResponse>(`/foods/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  archive: (id: number) =>
    request<FoodResponse>(`/foods/${id}/archive`, { method: 'PATCH' }),
  restore: (id: number) =>
    request<FoodResponse>(`/foods/${id}/restore`, { method: 'PATCH' }),
}

export const foodEntriesApi = {
  getByRange: (start: string, end: string) => {
    const query = new URLSearchParams({ start, end })
    return request<FoodEntryResponse[]>(`/food-entries?${query}`)
  },
  create: (payload: FoodEntryRequest) =>
    request<FoodEntryResponse>('/food-entries', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  update: (id: number, payload: FoodEntryRequest) =>
    request<FoodEntryResponse>(`/food-entries/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  delete: (id: number) =>
    request<void>(`/food-entries/${id}`, { method: 'DELETE' }),
}

export const dailyTargetApi = {
  getCurrent: () => request<DailyTargetResponse>('/daily-target'),
  update: (payload: UpdateDailyTargetRequest) =>
    request<DailyTargetResponse>('/daily-target', {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
}

export const dailySummaryApi = {
  get: (date: string) => {
    const query = new URLSearchParams({ date })
    return request<DailySummaryResponse>(`/daily-summary?${query}`)
  },
}
