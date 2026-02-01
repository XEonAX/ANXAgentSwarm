/**
 * API service for ANXAgentSwarm backend communication.
 * Provides typed methods for all API endpoints.
 */

import type {
  SessionDto,
  SessionDetailDto,
  CreateSessionRequest,
  ClarificationResponse,
  LlmStatusDto
} from '@/types'

/** Base URL for API endpoints */
const API_BASE = '/api'

/**
 * Custom API error with status code and message.
 */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly details?: unknown
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

/**
 * Generic fetch wrapper with error handling.
 */
async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE}${endpoint}`
  
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  })

  if (!response.ok) {
    let details: unknown
    try {
      details = await response.json()
    } catch {
      details = await response.text()
    }
    throw new ApiError(response.status, response.statusText, details)
  }

  // Handle empty responses (204 No Content)
  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}

// ============================================================================
// Session API
// ============================================================================

/**
 * Session-related API methods.
 */
export const sessionsApi = {
  /**
   * Fetches all sessions.
   */
  async getAll(): Promise<SessionDto[]> {
    return apiFetch<SessionDto[]>('/sessions')
  },

  /**
   * Fetches a single session with messages.
   */
  async getById(sessionId: string): Promise<SessionDetailDto> {
    return apiFetch<SessionDetailDto>(`/sessions/${sessionId}`)
  },

  /**
   * Creates a new session with the given problem statement.
   */
  async create(request: CreateSessionRequest): Promise<SessionDetailDto> {
    return apiFetch<SessionDetailDto>('/sessions', {
      method: 'POST',
      body: JSON.stringify(request)
    })
  },

  /**
   * Submits a clarification response for a session.
   */
  async submitClarification(
    sessionId: string,
    response: ClarificationResponse
  ): Promise<void> {
    return apiFetch<void>(`/sessions/${sessionId}/clarify`, {
      method: 'POST',
      body: JSON.stringify(response)
    })
  },

  /**
   * Cancels a session.
   */
  async cancel(sessionId: string): Promise<void> {
    return apiFetch<void>(`/sessions/${sessionId}/cancel`, {
      method: 'POST'
    })
  },

  /**
   * Resumes a paused or stuck session.
   */
  async resume(sessionId: string): Promise<void> {
    return apiFetch<void>(`/sessions/${sessionId}/resume`, {
      method: 'POST'
    })
  },

  /**
   * Deletes a session.
   */
  async delete(sessionId: string): Promise<void> {
    return apiFetch<void>(`/sessions/${sessionId}`, {
      method: 'DELETE'
    })
  }
}

// ============================================================================
// Status API
// ============================================================================

/**
 * Status-related API methods.
 */
export const statusApi = {
  /**
   * Health check endpoint.
   */
  async health(): Promise<{ status: string }> {
    return apiFetch<{ status: string }>('/status/health')
  },

  /**
   * Checks LLM availability and returns available models.
   */
  async llm(): Promise<LlmStatusDto> {
    return apiFetch<LlmStatusDto>('/status/llm')
  }
}

// ============================================================================
// Default Export
// ============================================================================

/**
 * Combined API service object.
 */
export const api = {
  sessions: sessionsApi,
  status: statusApi
}

export default api
