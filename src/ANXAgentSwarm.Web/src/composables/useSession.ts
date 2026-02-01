import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useSessionStore } from '@/stores/sessionStore'
import { useSignalR } from '@/composables/useSignalR'
import type { SessionDetailDto, MessageDto, PersonaType, SessionStatus } from '@/types'
import { SessionStatus as SessionStatusEnum } from '@/types'

/**
 * Composable for managing a single session's state and real-time updates.
 * This composable connects to SignalR and syncs with the Pinia store.
 */
export function useSession(sessionId?: string) {
  const store = useSessionStore()
  const signalR = useSignalR()
  
  // =========================================================================
  // State
  // =========================================================================
  
  const {
    currentSession,
    currentMessages,
    currentStatus,
    currentPersona,
    requiresClarification,
    isActive,
    isComplete,
    isLoading,
    error,
    isSubmittingClarification
  } = storeToRefs(store)
  
  /** The session ID we're currently watching */
  const activeSessionId = ref<string | null>(sessionId ?? null)
  
  /** Whether we're connected to SignalR for this session */
  const isConnected = ref(false)
  
  /** Latest clarification question (if any) */
  const clarificationQuestion = ref<string | null>(null)
  
  /** Persona asking for clarification */
  const clarificationPersona = ref<PersonaType | null>(null)
  
  /** Cleanup function for SignalR handlers */
  let cleanupHandlers: (() => void) | null = null

  // =========================================================================
  // Computed
  // =========================================================================
  
  /** Session title */
  const title = computed(() => currentSession.value?.title ?? '')
  
  /** Problem statement */
  const problemStatement = computed(() => currentSession.value?.problemStatement ?? '')
  
  /** Final solution if available */
  const finalSolution = computed(() => currentSession.value?.finalSolution ?? null)
  
  /** Message count */
  const messageCount = computed(() => currentMessages.value.length)
  
  /** Last message in the session */
  const lastMessage = computed<MessageDto | null>(() => {
    const messages = currentMessages.value
    return messages.length > 0 ? messages[messages.length - 1] ?? null : null
  })
  
  /** Status display text */
  const statusText = computed(() => {
    switch (currentStatus.value) {
      case SessionStatusEnum.Active:
        return 'Processing...'
      case SessionStatusEnum.WaitingForClarification:
        return 'Waiting for your input'
      case SessionStatusEnum.Completed:
        return 'Completed'
      case SessionStatusEnum.Stuck:
        return 'Stuck - needs attention'
      case SessionStatusEnum.Cancelled:
        return 'Cancelled'
      case SessionStatusEnum.Error:
        return 'Error occurred'
      case SessionStatusEnum.Interrupted:
        return 'Interrupted - can resume'
      default:
        return 'Unknown'
    }
  })

  // =========================================================================
  // Methods
  // =========================================================================
  
  /**
   * Loads a session by ID and connects to SignalR for updates.
   */
  async function loadSession(id: string): Promise<SessionDetailDto> {
    activeSessionId.value = id
    
    // Disconnect from previous session if any
    if (isConnected.value && activeSessionId.value) {
      await signalR.leaveSession(activeSessionId.value)
    }
    
    // Connect to SignalR if not already connected
    if (!signalR.isConnected()) {
      await signalR.connect()
    }
    
    // Register handlers
    setupSignalRHandlers()
    
    // Join the session room
    await signalR.joinSession(id)
    isConnected.value = true
    
    // Fetch session data
    return await store.fetchSession(id)
  }
  
  /**
   * Creates a new session and connects to it.
   * IMPORTANT: We connect to SignalR and set up handlers BEFORE the API call returns,
   * so we can receive real-time updates as the session is processed in the background.
   */
  async function createSession(problemStatement: string): Promise<SessionDetailDto> {
    // Connect to SignalR FIRST (before API call)
    if (!signalR.isConnected()) {
      await signalR.connect()
    }
    
    // Set up handlers before creating session so we catch all messages
    setupSignalRHandlers()
    
    // Create the session via API
    const session = await store.createSession(problemStatement)
    
    // Set active session ID
    activeSessionId.value = session.id
    
    // Join the session room ASAP to receive real-time updates
    await signalR.joinSession(session.id)
    isConnected.value = true
    
    return session
  }
  
  /**
   * Submits a clarification response.
   */
  async function submitClarification(response: string): Promise<void> {
    await store.submitClarification(response)
    clarificationQuestion.value = null
    clarificationPersona.value = null
  }
  
  /**
   * Cancels the current session.
   */
  async function cancelSession(): Promise<void> {
    await store.cancelSession()
  }

  /**
   * Resumes a stuck, interrupted, or error session.
   */
  async function resumeSession(): Promise<void> {
    await store.resumeSession()
  }
  
  /**
   * Sets up SignalR event handlers.
   */
  function setupSignalRHandlers(): void {
    // Clean up existing handlers
    if (cleanupHandlers) {
      cleanupHandlers()
    }
    
    cleanupHandlers = signalR.registerHandlers({
      onMessageReceived: (sessionId: string, message: MessageDto) => {
        if (sessionId === activeSessionId.value) {
          store.handleMessageReceived(sessionId, message)
        }
      },
      
      onSessionStatusChanged: (sessionId: string, status: SessionStatus, persona: PersonaType | null) => {
        if (sessionId === activeSessionId.value) {
          store.handleSessionStatusChanged(sessionId, status, persona)
        }
      },
      
      onClarificationRequested: (sessionId: string, question: string, fromPersona: PersonaType) => {
        if (sessionId === activeSessionId.value) {
          clarificationQuestion.value = question
          clarificationPersona.value = fromPersona
        }
      },
      
      onSolutionReady: (sessionId: string, solution: string) => {
        if (sessionId === activeSessionId.value) {
          store.handleSolutionReady(sessionId, solution)
        }
      },
      
      onSessionStuck: (sessionId: string, _partialResults: string, _reason: string) => {
        if (sessionId === activeSessionId.value) {
          store.handleSessionStatusChanged(sessionId, SessionStatusEnum.Stuck, null)
        }
      }
    })
  }
  
  /**
   * Disconnects from the current session.
   */
  async function disconnect(): Promise<void> {
    if (cleanupHandlers) {
      cleanupHandlers()
      cleanupHandlers = null
    }
    
    if (isConnected.value && activeSessionId.value) {
      await signalR.leaveSession(activeSessionId.value)
      isConnected.value = false
    }
  }

  /**
   * Clears the current session state (used when switching to new session mode).
   */
  async function clearSession(): Promise<void> {
    await disconnect()
    store.clearCurrentSession()
    activeSessionId.value = null
    clarificationQuestion.value = null
    clarificationPersona.value = null
  }
  
  /**
   * Refreshes the current session data from the server.
   */
  async function refresh(): Promise<void> {
    if (activeSessionId.value) {
      await store.fetchSession(activeSessionId.value)
    }
  }

  // =========================================================================
  // Lifecycle
  // =========================================================================
  
  // Watch for session ID changes
  watch(
    () => sessionId,
    async (newId) => {
      if (newId && newId !== activeSessionId.value) {
        await loadSession(newId)
      }
    },
    { immediate: false }
  )
  
  // Auto-load if session ID is provided
  onMounted(async () => {
    if (sessionId) {
      await loadSession(sessionId)
    }
  })
  
  // Cleanup on unmount
  onUnmounted(async () => {
    await disconnect()
  })

  return {
    // State
    currentSession,
    currentMessages,
    currentStatus,
    currentPersona,
    activeSessionId,
    isConnected,
    isLoading,
    error,
    isSubmittingClarification,
    clarificationQuestion,
    clarificationPersona,
    
    // Computed
    title,
    problemStatement,
    finalSolution,
    messageCount,
    lastMessage,
    statusText,
    requiresClarification,
    isActive,
    isComplete,
    
    // Methods
    loadSession,
    createSession,
    submitClarification,
    cancelSession,
    resumeSession,
    disconnect,
    clearSession,
    refresh
  }
}
