import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { MessageDto, PersonaType, MessageType } from '@/types'

/**
 * Pinia store for managing message state and operations.
 * Provides filtering, sorting, and message-specific utilities.
 */
export const useMessageStore = defineStore('message', () => {
  // =========================================================================
  // State
  // =========================================================================

  /** All messages for the current session */
  const messages = ref<MessageDto[]>([])

  /** Selected message ID for detail view */
  const selectedMessageId = ref<string | null>(null)

  /** Whether internal reasoning is shown globally */
  const showReasoningGlobally = ref(false)

  // =========================================================================
  // Getters
  // =========================================================================

  /** Messages sorted by timestamp (oldest first) */
  const sortedMessages = computed<MessageDto[]>(() =>
    [...messages.value].sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    )
  )

  /** Messages sorted by timestamp (newest first) */
  const sortedMessagesDesc = computed<MessageDto[]>(() =>
    [...messages.value].sort(
      (a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
    )
  )

  /** Total message count */
  const messageCount = computed(() => messages.value.length)

  /** The most recent message */
  const latestMessage = computed<MessageDto | null>(() => {
    const sorted = sortedMessages.value
    return sorted.length > 0 ? sorted[sorted.length - 1] ?? null : null
  })

  /** Messages that are stuck */
  const stuckMessages = computed<MessageDto[]>(() =>
    messages.value.filter(m => m.isStuck)
  )

  /** Messages that have internal reasoning */
  const messagesWithReasoning = computed<MessageDto[]>(() =>
    messages.value.filter(m => m.internalReasoning && m.internalReasoning.trim().length > 0)
  )

  /** Currently selected message */
  const selectedMessage = computed<MessageDto | null>(() => {
    if (!selectedMessageId.value) return null
    return messages.value.find(m => m.id === selectedMessageId.value) ?? null
  })

  // =========================================================================
  // Filter Methods
  // =========================================================================

  /**
   * Get messages from a specific persona.
   */
  function getMessagesByPersona(persona: PersonaType): MessageDto[] {
    return sortedMessages.value.filter(m => m.fromPersona === persona)
  }

  /**
   * Get messages of a specific type.
   */
  function getMessagesByType(type: MessageType): MessageDto[] {
    return sortedMessages.value.filter(m => m.messageType === type)
  }

  /**
   * Get messages in a delegation chain starting from a message.
   */
  function getDelegationChain(messageId: string): MessageDto[] {
    const chain: MessageDto[] = []
    const messageMap = new Map(messages.value.map(m => [m.id, m]))
    
    let currentId: string | null = messageId
    while (currentId) {
      const message = messageMap.get(currentId)
      if (message) {
        chain.push(message)
        // Find the next message in the chain (delegation response)
        const next = messages.value.find(m => m.parentMessageId === currentId)
        currentId = next?.id ?? null
      } else {
        break
      }
    }
    
    return chain
  }

  /**
   * Get unique personas that have participated in the conversation.
   */
  function getParticipatingPersonas(): PersonaType[] {
    const personas = new Set<PersonaType>()
    for (const message of messages.value) {
      personas.add(message.fromPersona)
    }
    return Array.from(personas)
  }

  // =========================================================================
  // Actions
  // =========================================================================

  /**
   * Sets the messages for the current session.
   */
  function setMessages(newMessages: MessageDto[]): void {
    messages.value = newMessages
  }

  /**
   * Adds a new message (prevents duplicates).
   */
  function addMessage(message: MessageDto): void {
    const exists = messages.value.some(m => m.id === message.id)
    if (!exists) {
      messages.value.push(message)
    }
  }

  /**
   * Updates an existing message.
   */
  function updateMessage(messageId: string, updates: Partial<MessageDto>): void {
    const index = messages.value.findIndex(m => m.id === messageId)
    if (index !== -1 && messages.value[index]) {
      messages.value[index] = { ...messages.value[index], ...updates } as MessageDto
    }
  }

  /**
   * Removes a message by ID.
   */
  function removeMessage(messageId: string): void {
    messages.value = messages.value.filter(m => m.id !== messageId)
  }

  /**
   * Clears all messages.
   */
  function clearMessages(): void {
    messages.value = []
    selectedMessageId.value = null
  }

  /**
   * Selects a message for detail view.
   */
  function selectMessage(messageId: string | null): void {
    selectedMessageId.value = messageId
  }

  /**
   * Toggles global reasoning visibility.
   */
  function toggleReasoningGlobally(): void {
    showReasoningGlobally.value = !showReasoningGlobally.value
  }

  return {
    // State
    messages,
    selectedMessageId,
    showReasoningGlobally,

    // Getters
    sortedMessages,
    sortedMessagesDesc,
    messageCount,
    latestMessage,
    stuckMessages,
    messagesWithReasoning,
    selectedMessage,

    // Filter Methods
    getMessagesByPersona,
    getMessagesByType,
    getDelegationChain,
    getParticipatingPersonas,

    // Actions
    setMessages,
    addMessage,
    updateMessage,
    removeMessage,
    clearMessages,
    selectMessage,
    toggleReasoningGlobally
  }
})
