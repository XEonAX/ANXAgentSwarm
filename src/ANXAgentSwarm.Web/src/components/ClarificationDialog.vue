<script setup lang="ts">
/**
 * ClarificationDialog - Modal dialog for handling clarification requests.
 * Displays the persona asking, their question, and provides an input for response.
 */
import { ref, computed, watch, nextTick } from 'vue'
import type { PersonaType } from '@/types'
import PersonaAvatar from '@/components/PersonaAvatar.vue'
import { getPersonaDisplayName, getPersonaStyle } from '@/utils/personaStyles'

const props = defineProps<{
  /** Whether the dialog is visible */
  modelValue: boolean
  /** The persona requesting clarification */
  persona: PersonaType | null
  /** The clarification question */
  question: string | null
  /** Whether submission is in progress */
  isSubmitting?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'submit', response: string): void
  (e: 'skip'): void
}>()

// Local state
const response = ref('')
const textareaRef = ref<HTMLTextAreaElement | null>(null)

// Computed values
const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const personaName = computed(() => 
  props.persona !== null ? getPersonaDisplayName(props.persona) : 'Unknown'
)

const personaStyle = computed(() => 
  props.persona !== null ? getPersonaStyle(props.persona) : null
)

const canSubmit = computed(() => 
  !props.isSubmitting && response.value.trim().length > 0
)

// Auto-resize textarea
function autoResize(): void {
  if (textareaRef.value) {
    textareaRef.value.style.height = 'auto'
    textareaRef.value.style.height = `${Math.min(textareaRef.value.scrollHeight, 200)}px`
  }
}

// Handle form submission
function handleSubmit(): void {
  if (!canSubmit.value) return
  emit('submit', response.value.trim())
  response.value = ''
}

// Handle skip/close
function handleSkip(): void {
  emit('skip')
  close()
}

// Close the dialog
function close(): void {
  isOpen.value = false
}

// Handle keyboard shortcuts
function handleKeydown(event: KeyboardEvent): void {
  if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
    event.preventDefault()
    handleSubmit()
  }
  if (event.key === 'Escape') {
    close()
  }
}

// Focus textarea when dialog opens
watch(isOpen, async (open) => {
  if (open) {
    response.value = ''
    await nextTick()
    textareaRef.value?.focus()
  }
})

// Reset height when response changes
watch(response, () => nextTick(autoResize))
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 flex items-center justify-center p-4"
        @keydown="handleKeydown"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-black/50 backdrop-blur-sm"
          @click="close"
        ></div>

        <!-- Dialog -->
        <Transition
          enter-active-class="transition-all duration-200 ease-out"
          enter-from-class="opacity-0 scale-95"
          enter-to-class="opacity-100 scale-100"
          leave-active-class="transition-all duration-150 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div
            v-if="isOpen"
            class="relative bg-white rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-hidden"
            role="dialog"
            aria-modal="true"
            aria-labelledby="clarification-title"
          >
            <!-- Header -->
            <div
              class="px-6 py-4 border-b"
              :class="personaStyle ? [personaStyle.bgLight, personaStyle.borderColor] : 'border-gray-200'"
            >
              <div class="flex items-start gap-3">
                <!-- Persona Avatar -->
                <PersonaAvatar
                  v-if="persona !== null"
                  :persona="persona"
                  size="lg"
                  :pulse="true"
                />

                <!-- Title and Question -->
                <div class="flex-1 min-w-0">
                  <h2
                    id="clarification-title"
                    class="text-lg font-semibold"
                    :class="personaStyle?.textColor ?? 'text-gray-900'"
                  >
                    {{ personaName }} needs clarification
                  </h2>
                  <span class="inline-flex items-center mt-1 px-2 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                    🤔 Your input needed
                  </span>
                </div>

                <!-- Close Button -->
                <button
                  @click="close"
                  class="flex-shrink-0 p-1 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                  title="Close"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>

            <!-- Question Content -->
            <div class="px-6 py-4">
              <div class="prose prose-sm max-w-none text-gray-700">
                <p class="font-medium text-gray-900 mb-2">Question:</p>
                <p class="whitespace-pre-wrap">{{ question }}</p>
              </div>
            </div>

            <!-- Response Input -->
            <div class="px-6 pb-6">
              <label for="clarification-response" class="block text-sm font-medium text-gray-700 mb-2">
                Your Response
              </label>
              <textarea
                id="clarification-response"
                ref="textareaRef"
                v-model="response"
                :disabled="isSubmitting"
                placeholder="Type your response to the clarification question..."
                rows="3"
                class="w-full px-4 py-3 border border-gray-300 rounded-lg resize-none focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:bg-gray-50 disabled:text-gray-500 transition-colors"
                @input="autoResize"
              />

              <!-- Character count -->
              <div class="mt-1 flex justify-between items-center text-xs text-gray-400">
                <span>
                  Press <kbd class="px-1 py-0.5 bg-gray-100 rounded font-mono">⌘</kbd>+<kbd class="px-1 py-0.5 bg-gray-100 rounded font-mono">Enter</kbd> to submit
                </span>
                <span v-if="response.length > 0">{{ response.length }} characters</span>
              </div>
            </div>

            <!-- Footer Actions -->
            <div class="px-6 py-4 bg-gray-50 border-t border-gray-200 flex justify-end gap-3">
              <button
                @click="handleSkip"
                :disabled="isSubmitting"
                class="px-4 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50"
              >
                Skip
              </button>
              <button
                @click="handleSubmit"
                :disabled="!canSubmit"
                class="px-4 py-2 text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <svg
                  v-if="isSubmitting"
                  class="animate-spin w-4 h-4"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                {{ isSubmitting ? 'Sending...' : 'Send Response' }}
              </button>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
