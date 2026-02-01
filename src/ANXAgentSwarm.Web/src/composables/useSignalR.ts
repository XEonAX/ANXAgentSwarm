import { ref, shallowRef, readonly } from 'vue'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr'
import type {
  MessageDto,
  SessionStatus,
  PersonaType,
  MessageReceivedEvent,
  SessionStatusChangedEvent,
  ClarificationRequestedEvent,
  SolutionReadyEvent,
  SessionStuckEvent
} from '@/types'

/**
 * Connection state for the SignalR hub.
 */
export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'

/**
 * Event handlers that can be registered with the SignalR hub.
 */
export interface SignalREventHandlers {
  onMessageReceived?: (sessionId: string, message: MessageDto) => void
  onSessionStatusChanged?: (sessionId: string, status: SessionStatus, currentPersona: PersonaType | null) => void
  onClarificationRequested?: (sessionId: string, question: string, fromPersona: PersonaType) => void
  onSolutionReady?: (sessionId: string, solution: string) => void
  onSessionStuck?: (sessionId: string, partialResults: string, reason: string) => void
}

// Singleton connection instance
let connectionInstance: HubConnection | null = null
let connectionPromise: Promise<void> | null = null

/**
 * Composable for managing SignalR connection to the session hub.
 * Uses a singleton pattern to ensure only one connection exists.
 */
export function useSignalR() {
  const connectionState = ref<ConnectionState>('disconnected')
  const error = ref<Error | null>(null)
  const connection = shallowRef<HubConnection | null>(null)

  /**
   * Creates or returns the singleton hub connection.
   */
  function getOrCreateConnection(): HubConnection {
    if (connectionInstance) {
      connection.value = connectionInstance
      return connectionInstance
    }

    connectionInstance = new HubConnectionBuilder()
      .withUrl('/hubs/session')
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2, 10, 30 seconds, then every 30 seconds
          if (retryContext.previousRetryCount === 0) return 0
          if (retryContext.previousRetryCount === 1) return 2000
          if (retryContext.previousRetryCount === 2) return 10000
          return 30000
        }
      })
      .configureLogging(LogLevel.Information)
      .build()

    // Set up connection state handlers
    connectionInstance.onreconnecting(() => {
      connectionState.value = 'reconnecting'
    })

    connectionInstance.onreconnected(() => {
      connectionState.value = 'connected'
      error.value = null
    })

    connectionInstance.onclose((err) => {
      connectionState.value = 'disconnected'
      if (err) {
        error.value = err
      }
    })

    connection.value = connectionInstance
    return connectionInstance
  }

  /**
   * Connects to the SignalR hub.
   * Returns a promise that resolves when connected.
   */
  async function connect(): Promise<void> {
    const conn = getOrCreateConnection()

    // If already connected, return immediately
    if (conn.state === HubConnectionState.Connected) {
      connectionState.value = 'connected'
      return
    }

    // If already connecting, wait for that promise
    if (connectionPromise) {
      return connectionPromise
    }

    connectionState.value = 'connecting'
    error.value = null

    connectionPromise = conn.start()
      .then(() => {
        connectionState.value = 'connected'
        connectionPromise = null
      })
      .catch((err: Error) => {
        connectionState.value = 'error'
        error.value = err
        connectionPromise = null
        throw err
      })

    return connectionPromise
  }

  /**
   * Disconnects from the SignalR hub.
   */
  async function disconnect(): Promise<void> {
    if (connectionInstance) {
      await connectionInstance.stop()
      connectionState.value = 'disconnected'
    }
  }

  /**
   * Joins a session to receive real-time updates.
   */
  async function joinSession(sessionId: string): Promise<void> {
    const conn = getOrCreateConnection()
    if (conn.state !== HubConnectionState.Connected) {
      await connect()
    }
    await conn.invoke('JoinSession', sessionId)
  }

  /**
   * Leaves a session to stop receiving updates.
   */
  async function leaveSession(sessionId: string): Promise<void> {
    const conn = getOrCreateConnection()
    if (conn.state === HubConnectionState.Connected) {
      await conn.invoke('LeaveSession', sessionId)
    }
  }

  /**
   * Registers event handlers for SignalR events.
   * Returns a cleanup function to unregister the handlers.
   */
  function registerHandlers(handlers: SignalREventHandlers): () => void {
    const conn = getOrCreateConnection()

    if (handlers.onMessageReceived) {
      conn.on('MessageReceived', (event: MessageReceivedEvent) => {
        handlers.onMessageReceived!(event.sessionId, event.message)
      })
    }

    if (handlers.onSessionStatusChanged) {
      conn.on('SessionStatusChanged', (event: SessionStatusChangedEvent) => {
        handlers.onSessionStatusChanged!(event.sessionId, event.status, event.currentPersona)
      })
    }

    if (handlers.onClarificationRequested) {
      conn.on('ClarificationRequested', (event: ClarificationRequestedEvent) => {
        handlers.onClarificationRequested!(event.sessionId, event.question, event.fromPersona)
      })
    }

    if (handlers.onSolutionReady) {
      conn.on('SolutionReady', (event: SolutionReadyEvent) => {
        handlers.onSolutionReady!(event.sessionId, event.solution)
      })
    }

    if (handlers.onSessionStuck) {
      conn.on('SessionStuck', (event: SessionStuckEvent) => {
        handlers.onSessionStuck!(event.sessionId, event.partialResults, event.reason)
      })
    }

    // Return cleanup function
    return () => {
      if (handlers.onMessageReceived) {
        conn.off('MessageReceived')
      }
      if (handlers.onSessionStatusChanged) {
        conn.off('SessionStatusChanged')
      }
      if (handlers.onClarificationRequested) {
        conn.off('ClarificationRequested')
      }
      if (handlers.onSolutionReady) {
        conn.off('SolutionReady')
      }
      if (handlers.onSessionStuck) {
        conn.off('SessionStuck')
      }
    }
  }

  /**
   * Checks if the connection is currently connected.
   */
  function isConnected(): boolean {
    return connectionInstance?.state === HubConnectionState.Connected
  }

  return {
    // State
    connectionState: readonly(connectionState),
    error: readonly(error),
    connection: readonly(connection),
    
    // Methods
    connect,
    disconnect,
    joinSession,
    leaveSession,
    registerHandlers,
    isConnected
  }
}
