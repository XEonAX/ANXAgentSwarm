<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useSession } from '@/composables/useSession'
import Timeline from '@/components/Timeline.vue'
import UserInput from '@/components/UserInput.vue'
import SessionHeader from '@/components/SessionHeader.vue'
import SolutionViewer from '@/components/SolutionViewer.vue'
import { SessionStatus } from '@/types'

const props = defineProps<{
  sessionId?: string | null
}>()

const emit = defineEmits<{
  (e: 'back'): void
  (e: 'session-created', sessionId: string): void
}>()

// Use the session composable
const {
  currentSession,
  currentMessages,
  currentStatus,
  currentPersona,
  isLoading,
  error,
  isSubmittingClarification,
  clarificationQuestion,
  clarificationPersona,
  requiresClarification,
  isActive,
  isComplete,
  title,
  finalSolution,
  statusText,
  loadSession,
  createSession,
  submitClarification,
  cancelSession,
  resumeSession,
  clearSession
} = useSession()

// Local state for creating new session
const isCreatingSession = ref(false)

// Local state for resuming session
const isResuming = ref(false)

// Computed: determine input mode
const inputMode = computed<'new' | 'clarification'>(() => {
  if (!props.sessionId) return 'new'
  return requiresClarification.value ? 'clarification' : 'new'
})

// Computed: should show input
const showInput = computed(() => {
  // Show input for new session
  if (!props.sessionId) return true
  // Show input when waiting for clarification
  if (requiresClarification.value) return true
  // Hide input for completed/stuck/cancelled/error sessions
  return false
})

// Computed: is input disabled
const isInputDisabled = computed(() => {
  if (!props.sessionId) return false
  // Disable when session is actively processing (not waiting for clarification)
  return isActive.value && !requiresClarification.value
})

// Load session when ID changes, or clear when switching to new session mode
watch(
  () => props.sessionId,
  async (newId) => {
    if (newId) {
      await loadSession(newId)
    } else {
      // Switching to new session mode - clear the previous session
      await clearSession()
    }
  },
  { immediate: true }
)

// Handle new session submission
async function handleNewSession(problemStatement: string): Promise<void> {
  isCreatingSession.value = true
  try {
    const session = await createSession(problemStatement)
    emit('session-created', session.id)
  } catch (err) {
    console.error('Failed to create session:', err)
  } finally {
    isCreatingSession.value = false
  }
}

// Handle clarification response
async function handleClarificationResponse(response: string): Promise<void> {
  await submitClarification(response)
}

// Handle input submission (routes to correct handler)
function handleSubmit(value: string): void {
  if (inputMode.value === 'new') {
    handleNewSession(value)
  } else {
    handleClarificationResponse(value)
  }
}

// Handle cancel button click
function handleCancel(): void {
  // For now, just clear the clarification state
  // Could also implement cancel session functionality
}

// Handle back button
function handleBack(): void {
  emit('back')
}

// Handle cancel session
async function handleCancelSession(): Promise<void> {
  if (confirm('Are you sure you want to cancel this session?')) {
    await cancelSession()
  }
}

// Handle resume session
async function handleResumeSession(): Promise<void> {
  isResuming.value = true
  try {
    await resumeSession()
  } catch (err) {
    console.error('Failed to resume session:', err)
  } finally {
    isResuming.value = false
  }
}
</script>

<template>
  <div class="flex flex-col h-full bg-white">
    <!-- Session Header (for existing sessions) -->
    <SessionHeader
      v-if="sessionId && currentSession"
      :title="title"
      :status="currentStatus"
      :status-text="statusText"
      :current-persona="currentPersona"
      :is-loading="isLoading"
      @back="handleBack"
      @cancel="handleCancelSession"
    />

    <!-- New Session Header -->
    <div
      v-else-if="!sessionId"
      class="flex-shrink-0 px-6 py-4 border-b border-gray-200"
    >
      <h2 class="text-xl font-bold text-gray-900">New Session</h2>
      <p class="text-sm text-gray-500 mt-1">
        Describe your problem and let the AI agents work together to solve it.
      </p>
    </div>

    <!-- Error State -->
    <div
      v-if="error"
      class="flex-shrink-0 mx-4 mt-4 p-4 bg-red-50 border border-red-200 rounded-lg"
    >
      <div class="flex items-start gap-3">
        <span class="text-red-500 text-lg">❌</span>
        <div>
          <h4 class="font-medium text-red-800">Error</h4>
          <p class="text-sm text-red-700">{{ error.message }}</p>
        </div>
      </div>
    </div>

    <!-- Final Solution Banner -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 -translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div
        v-if="isComplete && finalSolution"
        class="flex-shrink-0 mx-4 mt-4"
      >
        <SolutionViewer
          :solution="finalSolution"
          title="Solution Ready"
        />
      </div>
    </Transition>

    <!-- Stuck Banner -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 -translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div
        v-if="currentStatus === SessionStatus.Stuck"
        class="flex-shrink-0 mx-4 mt-4 p-4 bg-orange-50 border border-orange-200 rounded-lg"
      >
        <div class="flex items-start gap-3">
          <span class="text-orange-500 text-2xl">⚠️</span>
          <div class="flex-1 min-w-0">
            <h4 class="font-semibold text-orange-800 mb-1">Session Stuck</h4>
            <p class="text-sm text-orange-700 mb-3">
              The AI agents were unable to complete this task. You can try resuming to let them
              attempt a different approach, or review the partial results in the timeline.
            </p>
            <button
              @click="handleResumeSession"
              :disabled="isResuming"
              class="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span v-if="isResuming">Resuming...</span>
              <span v-else>🔄 Resume Session</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Interrupted Banner -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 -translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div
        v-if="currentStatus === SessionStatus.Interrupted"
        class="flex-shrink-0 mx-4 mt-4 p-4 bg-purple-50 border border-purple-200 rounded-lg"
      >
        <div class="flex items-start gap-3">
          <span class="text-purple-500 text-2xl">⏸️</span>
          <div class="flex-1 min-w-0">
            <h4 class="font-semibold text-purple-800 mb-1">Session Interrupted</h4>
            <p class="text-sm text-purple-700 mb-3">
              This session was interrupted while processing (e.g., due to a server restart).
              Click Resume to continue where it left off.
            </p>
            <button
              @click="handleResumeSession"
              :disabled="isResuming"
              class="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span v-if="isResuming">Resuming...</span>
              <span v-else>▶️ Resume Session</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Error Banner with Resume -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 -translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div
        v-if="currentStatus === SessionStatus.Error"
        class="flex-shrink-0 mx-4 mt-4 p-4 bg-red-50 border border-red-200 rounded-lg"
      >
        <div class="flex items-start gap-3">
          <span class="text-red-500 text-2xl">❌</span>
          <div class="flex-1 min-w-0">
            <h4 class="font-semibold text-red-800 mb-1">Session Error</h4>
            <p class="text-sm text-red-700 mb-3">
              This session encountered an error during processing.
              You can try resuming to retry from where it left off.
            </p>
            <button
              @click="handleResumeSession"
              :disabled="isResuming"
              class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span v-if="isResuming">Retrying...</span>
              <span v-else>🔄 Retry Session</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Timeline -->
    <div class="flex-1 min-h-0">
      <Timeline
        ref="timelineRef"
        :messages="currentMessages"
        :is-loading="isLoading"
        :current-persona="isActive ? currentPersona : null"
        :auto-scroll="true"
      />
    </div>

    <!-- User Input -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 translate-y-4"
    >
      <UserInput
        v-if="showInput"
        :mode="inputMode"
        :disabled="isInputDisabled"
        :is-submitting="isCreatingSession || isSubmittingClarification"
        :clarification-question="clarificationQuestion"
        :clarification-persona="clarificationPersona"
        @submit="handleSubmit"
        @cancel="handleCancel"
      />
    </Transition>
  </div>
</template>
