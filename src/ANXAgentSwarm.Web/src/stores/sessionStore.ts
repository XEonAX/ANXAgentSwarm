import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  SessionDto,
  SessionDetailDto,
  MessageDto,
  SessionStatus,
  PersonaType
} from '@/types'

/**
 * API base URL for session endpoints.
 */
const API_BASE = '/api/sessions'

/**
 * Pinia store for managing session state.
 */
export const useSessionStore = defineStore('session', () => {
  // =========================================================================
  // State
  // =========================================================================
  
  /** List of all sessions (summary view) */
  const sessions = ref<SessionDto[]>([])
  
  /** Currently active session with full details */
  const currentSession = ref<SessionDetailDto | null>(null)
  
  /** Loading state for async operations */
  const isLoading = ref(false)
  
  /** Error from the last operation */
  const error = ref<Error | null>(null)
  
  /** Whether we're currently submitting a clarification response */
  const isSubmittingClarification = ref(false)

  // =========================================================================
  // Getters
  // =========================================================================
  
  /** Messages for the current session, sorted by timestamp */
  const currentMessages = computed<MessageDto[]>(() => {
    if (!currentSession.value?.messages) return []
    return [...currentSession.value.messages].sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    )
  })

  /** Current session status */
  const currentStatus = computed<SessionStatus | null>(() => {
    return currentSession.value?.status ?? null
  })

  /** Current persona processing the session */
  const currentPersona = computed<PersonaType | null>(() => {
    return currentSession.value?.currentPersona ?? null
  })

  /** Whether the current session requires user input */
  const requiresClarification = computed<boolean>(() => {
    return currentSession.value?.status === 1 // WaitingForClarification
  })

  /** Whether the current session is actively processing */
  const isActive = computed<boolean>(() => {
    return currentSession.value?.status === 0 // Active
  })

  /** Whether the current session is complete */
  const isComplete = computed<boolean>(() => {
    return currentSession.value?.status === 2 // Completed
  })

  // =========================================================================
  // Actions
  // =========================================================================

  /**
   * Fetches all sessions from the API.
   */
  async function fetchSessions(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(API_BASE)
      if (!response.ok) {
        throw new Error(`Failed to fetch sessions: ${response.statusText}`)
      }
      sessions.value = await response.json()
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Fetches a specific session with full details.
   */
  async function fetchSession(sessionId: string): Promise<SessionDetailDto> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(`${API_BASE}/${sessionId}`)
      if (!response.ok) {
        throw new Error(`Failed to fetch session: ${response.statusText}`)
      }
      const session: SessionDetailDto = await response.json()
      currentSession.value = session
      return session
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Creates a new session with the given problem statement.
   */
  async function createSession(problemStatement: string): Promise<SessionDetailDto> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(API_BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ problemStatement })
      })
      
      if (!response.ok) {
        throw new Error(`Failed to create session: ${response.statusText}`)
      }
      
      const session: SessionDetailDto = await response.json()
      currentSession.value = session
      
      // Add to sessions list
      sessions.value.unshift({
        id: session.id,
        title: session.title,
        status: session.status,
        problemStatement: session.problemStatement,
        finalSolution: session.finalSolution,
        createdAt: session.createdAt,
        updatedAt: session.updatedAt,
        messageCount: session.messages.length
      })
      
      return session
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Submits a clarification response for the current session.
   */
  async function submitClarification(response: string): Promise<void> {
    if (!currentSession.value) {
      throw new Error('No active session')
    }

    isSubmittingClarification.value = true
    error.value = null

    try {
      const res = await fetch(`${API_BASE}/${currentSession.value.id}/clarify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ response })
      })
      
      if (!res.ok) {
        throw new Error(`Failed to submit clarification: ${res.statusText}`)
      }
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isSubmittingClarification.value = false
    }
  }

  /**
   * Cancels the current session.
   */
  async function cancelSession(): Promise<void> {
    if (!currentSession.value) {
      throw new Error('No active session')
    }

    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(`${API_BASE}/${currentSession.value.id}/cancel`, {
        method: 'POST'
      })
      
      if (!response.ok) {
        throw new Error(`Failed to cancel session: ${response.statusText}`)
      }
      
      // Update local state
      if (currentSession.value) {
        currentSession.value.status = 4 // Cancelled
      }
      
      // Update in sessions list
      const idx = sessions.value.findIndex(s => s.id === currentSession.value?.id)
      if (idx !== -1 && sessions.value[idx]) {
        sessions.value[idx]!.status = 4 // Cancelled
      }
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Deletes a session.
   */
  async function deleteSession(sessionId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(`${API_BASE}/${sessionId}`, {
        method: 'DELETE'
      })
      
      if (!response.ok) {
        throw new Error(`Failed to delete session: ${response.statusText}`)
      }
      
      // Remove from local state
      sessions.value = sessions.value.filter(s => s.id !== sessionId)
      
      if (currentSession.value?.id === sessionId) {
        currentSession.value = null
      }
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
      throw error.value
    } finally {
      isLoading.value = false
    }
  }

  // =========================================================================
  // Real-time update handlers (called by SignalR)
  // =========================================================================

  /**
   * Handles a new message received via SignalR.
   */
  function handleMessageReceived(sessionId: string, message: MessageDto): void {
    // Update current session if it matches
    if (currentSession.value?.id === sessionId) {
      // Check if message already exists (prevent duplicates)
      const exists = currentSession.value.messages.some(m => m.id === message.id)
      if (!exists) {
        currentSession.value.messages.push(message)
      }
    }
    
    // Update message count in sessions list
    const sessionIdx = sessions.value.findIndex(s => s.id === sessionId)
    if (sessionIdx !== -1 && sessions.value[sessionIdx]) {
      sessions.value[sessionIdx]!.messageCount++
      sessions.value[sessionIdx]!.updatedAt = new Date().toISOString()
    }
  }

  /**
   * Handles session status change via SignalR.
   */
  function handleSessionStatusChanged(
    sessionId: string,
    status: SessionStatus,
    persona: PersonaType | null
  ): void {
    // Update current session if it matches
    if (currentSession.value?.id === sessionId) {
      currentSession.value.status = status
      currentSession.value.currentPersona = persona
    }
    
    // Update in sessions list
    const sessionIdx = sessions.value.findIndex(s => s.id === sessionId)
    if (sessionIdx !== -1 && sessions.value[sessionIdx]) {
      sessions.value[sessionIdx]!.status = status
      sessions.value[sessionIdx]!.updatedAt = new Date().toISOString()
    }
  }

  /**
   * Handles solution ready event via SignalR.
   */
  function handleSolutionReady(sessionId: string, solution: string): void {
    // Update current session if it matches
    if (currentSession.value?.id === sessionId) {
      currentSession.value.finalSolution = solution
      currentSession.value.status = 2 // Completed
    }
    
    // Update in sessions list
    const sessionIdx = sessions.value.findIndex(s => s.id === sessionId)
    if (sessionIdx !== -1 && sessions.value[sessionIdx]) {
      sessions.value[sessionIdx]!.finalSolution = solution
      sessions.value[sessionIdx]!.status = 2 // Completed
      sessions.value[sessionIdx]!.updatedAt = new Date().toISOString()
    }
  }

  /**
   * Clears the current session.
   */
  function clearCurrentSession(): void {
    currentSession.value = null
  }

  /**
   * Clears any error state.
   */
  function clearError(): void {
    error.value = null
  }

  return {
    // State
    sessions,
    currentSession,
    isLoading,
    error,
    isSubmittingClarification,
    
    // Getters
    currentMessages,
    currentStatus,
    currentPersona,
    requiresClarification,
    isActive,
    isComplete,
    
    // Actions
    fetchSessions,
    fetchSession,
    createSession,
    submitClarification,
    cancelSession,
    deleteSession,
    
    // Real-time handlers
    handleMessageReceived,
    handleSessionStatusChanged,
    handleSolutionReady,
    
    // Utility
    clearCurrentSession,
    clearError
  }
})
