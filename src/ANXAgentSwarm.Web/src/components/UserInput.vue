<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import type { PersonaType } from '@/types'
import { getPersonaStyle, getPersonaDisplayName } from '@/utils/personaStyles'

const props = defineProps<{
  /** Mode of input: 'new' for new session, 'clarification' for responding to questions */
  mode: 'new' | 'clarification'
  /** Whether input is disabled */
  disabled?: boolean
  /** Whether submission is in progress */
  isSubmitting?: boolean
  /** Placeholder text override */
  placeholder?: string
  /** Clarification question being asked */
  clarificationQuestion?: string | null
  /** Persona asking for clarification */
  clarificationPersona?: PersonaType | null
}>()

const emit = defineEmits<{
  (e: 'submit', value: string): void
  (e: 'cancel'): void
}>()

// Input value
const inputValue = ref('')

// Textarea ref for auto-resize and focus
const textareaRef = ref<HTMLTextAreaElement | null>(null)

// Computed placeholder based on mode
const computedPlaceholder = computed(() => {
  if (props.placeholder) return props.placeholder
  return props.mode === 'new'
    ? 'Describe your problem or task for the AI agents to solve...'
    : 'Type your response to the clarification question...'
})

// Computed button text
const buttonText = computed(() => {
  if (props.isSubmitting) {
    return props.mode === 'new' ? 'Creating...' : 'Sending...'
  }
  return props.mode === 'new' ? 'Start Session' : 'Send Response'
})

// Computed submit disabled state
const isSubmitDisabled = computed(() => {
  return props.disabled || props.isSubmitting || inputValue.value.trim().length === 0
})

// Persona style for clarification mode
const personaInfo = computed(() => {
  if (props.clarificationPersona === null || props.clarificationPersona === undefined) return null
  return {
    name: getPersonaDisplayName(props.clarificationPersona),
    style: getPersonaStyle(props.clarificationPersona)
  }
})

// Auto-resize textarea
function autoResize(): void {
  if (textareaRef.value) {
    textareaRef.value.style.height = 'auto'
    textareaRef.value.style.height = `${Math.min(textareaRef.value.scrollHeight, 200)}px`
  }
}

// Handle form submission
function handleSubmit(): void {
  if (isSubmitDisabled.value) return
  
  const value = inputValue.value.trim()
  if (value) {
    emit('submit', value)
    inputValue.value = ''
    nextTick(autoResize)
  }
}

// Handle keyboard shortcuts
function handleKeydown(event: KeyboardEvent): void {
  // Submit on Cmd/Ctrl + Enter
  if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
    event.preventDefault()
    handleSubmit()
  }
}

// Handle cancel in clarification mode
function handleCancel(): void {
  emit('cancel')
}

// Focus input when mode changes to clarification
watch(
  () => props.mode,
  async (newMode) => {
    if (newMode === 'clarification') {
      await nextTick()
      textareaRef.value?.focus()
    }
  }
)

// Reset height when value changes
watch(inputValue, () => {
  nextTick(autoResize)
})

// Expose focus method
defineExpose({
  focus: () => textareaRef.value?.focus()
})
</script>

<template>
  <div class="border-t border-gray-200 bg-white">
    <!-- Clarification Question Banner -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 -translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 -translate-y-2"
    >
      <div
        v-if="mode === 'clarification' && clarificationQuestion && personaInfo"
        class="px-4 py-3 border-b"
        :class="[personaInfo.style.bgLight, personaInfo.style.borderColor]"
      >
        <div class="flex items-start gap-2">
          <!-- Persona Icon -->
          <div
            class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center text-white text-sm"
            :class="`bg-gradient-to-br ${personaInfo.style.gradient}`"
          >
            {{ personaInfo.style.icon }}
          </div>
          
          <!-- Question Content -->
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <span class="font-medium text-sm" :class="personaInfo.style.textColor">
                {{ personaInfo.name }} is asking:
              </span>
              <span
                class="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800"
              >
                🤔 Clarification Needed
              </span>
            </div>
            <p class="text-sm text-gray-700">{{ clarificationQuestion }}</p>
          </div>

          <!-- Close Button -->
          <button
            @click="handleCancel"
            class="flex-shrink-0 p-1 text-gray-400 hover:text-gray-600 transition-colors"
            title="Skip this question"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>
      </div>
    </Transition>

    <!-- Input Area -->
    <div class="p-4">
      <div class="flex gap-3">
        <!-- Text Input -->
        <div class="flex-1 relative">
          <textarea
            ref="textareaRef"
            v-model="inputValue"
            :placeholder="computedPlaceholder"
            :disabled="disabled || isSubmitting"
            @keydown="handleKeydown"
            @input="autoResize"
            rows="1"
            class="w-full px-4 py-3 border border-gray-300 rounded-lg resize-none focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:bg-gray-50 disabled:text-gray-500 transition-colors"
            :class="{ 'pr-12': mode === 'new' }"
          />
          
          <!-- Character count hint -->
          <div
            v-if="inputValue.length > 0"
            class="absolute bottom-2 right-2 text-xs text-gray-400"
          >
            {{ inputValue.length }}
          </div>
        </div>

        <!-- Submit Button -->
        <button
          @click="handleSubmit"
          :disabled="isSubmitDisabled"
          class="flex-shrink-0 px-5 py-3 font-medium rounded-lg transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-offset-2"
          :class="[
            isSubmitDisabled
              ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
              : 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500'
          ]"
        >
          <!-- Loading Spinner -->
          <svg
            v-if="isSubmitting"
            class="animate-spin -ml-1 mr-2 h-4 w-4 inline-block"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              class="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              stroke-width="4"
            />
            <path
              class="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          {{ buttonText }}
        </button>
      </div>

      <!-- Keyboard shortcut hint -->
      <p class="mt-2 text-xs text-gray-400 text-right">
        Press <kbd class="px-1.5 py-0.5 bg-gray-100 rounded text-gray-600 font-mono">⌘</kbd>
        <kbd class="px-1.5 py-0.5 bg-gray-100 rounded text-gray-600 font-mono">Enter</kbd>
        to submit
      </p>
    </div>
  </div>
</template>

<style scoped>
textarea {
  min-height: 48px;
  max-height: 200px;
  line-height: 1.5;
}

textarea::-webkit-scrollbar {
  width: 6px;
}

textarea::-webkit-scrollbar-track {
  background: transparent;
}

textarea::-webkit-scrollbar-thumb {
  background-color: #d1d5db;
  border-radius: 3px;
}
</style>
