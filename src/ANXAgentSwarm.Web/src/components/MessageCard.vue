<script setup lang="ts">
import { ref, computed } from 'vue'
import type { MessageDto } from '@/types'
import { MessageType, PersonaType } from '@/types'
import {
  getPersonaStyle,
  getPersonaDisplayName,
  getMessageTypeStyle,
  formatTimestampWithTime
} from '@/utils/personaStyles'

const props = defineProps<{
  message: MessageDto
  isLatest?: boolean
}>()

const emit = defineEmits<{
  (e: 'persona-click', persona: PersonaType): void
}>()

// State for toggling internal reasoning visibility
const showReasoning = ref(false)

// Computed styles based on persona
const personaStyle = computed(() => getPersonaStyle(props.message.fromPersona))
const personaName = computed(() => getPersonaDisplayName(props.message.fromPersona))
const messageTypeStyle = computed(() => getMessageTypeStyle(props.message.messageType))
const formattedTime = computed(() => formatTimestampWithTime(props.message.timestamp))

// Check if this is a user message
const isUserMessage = computed(() => props.message.fromPersona === PersonaType.User)

// Check if message has internal reasoning
const hasReasoning = computed(() => 
  props.message.internalReasoning && props.message.internalReasoning.trim().length > 0
)

// Check if this is a delegation message
const isDelegation = computed(() => props.message.messageType === MessageType.Delegation)

// Get target persona for delegation
const targetPersonaName = computed(() => {
  if (props.message.delegateToPersona !== null && props.message.delegateToPersona !== undefined) {
    return getPersonaDisplayName(props.message.delegateToPersona)
  }
  return null
})

const targetPersonaStyle = computed(() => {
  if (props.message.delegateToPersona !== null && props.message.delegateToPersona !== undefined) {
    return getPersonaStyle(props.message.delegateToPersona)
  }
  return null
})

function toggleReasoning(): void {
  showReasoning.value = !showReasoning.value
}

function handlePersonaClick(): void {
  emit('persona-click', props.message.fromPersona)
}
</script>

<template>
  <div
    class="group relative flex gap-3 py-4 transition-all duration-200"
    :class="[
      isLatest ? 'animate-fade-in' : '',
      isUserMessage ? 'flex-row-reverse' : ''
    ]"
  >
    <!-- Avatar -->
    <button
      @click="handlePersonaClick"
      class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center text-white font-bold text-sm shadow-md transition-transform hover:scale-110 focus:outline-none focus:ring-2 focus:ring-offset-2"
      :class="[`bg-gradient-to-br ${personaStyle.gradient}`, personaStyle.ringColor]"
      :title="personaName"
    >
      <span class="text-lg">{{ personaStyle.icon }}</span>
    </button>

    <!-- Message Content -->
    <div
      class="flex-1 min-w-0 max-w-3xl"
      :class="isUserMessage ? 'text-right' : ''"
    >
      <!-- Header -->
      <div
        class="flex items-center gap-2 mb-1"
        :class="isUserMessage ? 'justify-end' : ''"
      >
        <span class="font-semibold text-sm" :class="personaStyle.textColor">
          {{ personaName }}
        </span>
        
        <!-- Message Type Badge -->
        <span
          class="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium"
          :class="[messageTypeStyle.bgColor, messageTypeStyle.textColor]"
        >
          <span class="mr-0.5">{{ messageTypeStyle.icon }}</span>
          {{ messageTypeStyle.label }}
        </span>

        <!-- Delegation Arrow -->
        <template v-if="isDelegation && targetPersonaName">
          <span class="text-gray-400">→</span>
          <span class="font-medium text-sm" :class="targetPersonaStyle?.textColor">
            {{ targetPersonaName }}
          </span>
        </template>

        <span class="text-xs text-gray-400 ml-auto">
          {{ formattedTime }}
        </span>
      </div>

      <!-- Message Body -->
      <div
        class="rounded-lg p-4 shadow-sm border transition-colors"
        :class="[
          personaStyle.bgLight,
          personaStyle.borderColor,
          isUserMessage ? 'ml-auto' : ''
        ]"
      >
        <!-- Main Content -->
        <div class="prose prose-sm max-w-none text-gray-800 whitespace-pre-wrap">
          {{ message.content }}
        </div>

        <!-- Delegation Context -->
        <div
          v-if="isDelegation && message.delegationContext"
          class="mt-3 pt-3 border-t border-gray-200"
        >
          <div class="text-xs font-medium text-gray-500 mb-1">Delegation Context:</div>
          <div class="text-sm text-gray-700 italic">
            {{ message.delegationContext }}
          </div>
        </div>

        <!-- Stuck Indicator -->
        <div
          v-if="message.isStuck"
          class="mt-3 p-2 bg-orange-100 border border-orange-300 rounded-md"
        >
          <span class="text-sm text-orange-800 font-medium">
            ⚠️ This persona is stuck and cannot proceed
          </span>
        </div>
      </div>

      <!-- Internal Reasoning Toggle -->
      <div v-if="hasReasoning" class="mt-2">
        <button
          @click="toggleReasoning"
          class="text-xs font-medium text-gray-500 hover:text-gray-700 transition-colors flex items-center gap-1"
          :class="isUserMessage ? 'ml-auto' : ''"
        >
          <svg
            class="w-3.5 h-3.5 transition-transform"
            :class="showReasoning ? 'rotate-90' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 5l7 7-7 7"
            />
          </svg>
          {{ showReasoning ? 'Hide' : 'Show' }} Internal Reasoning
        </button>

        <!-- Internal Reasoning Content -->
        <transition
          enter-active-class="transition-all duration-200 ease-out"
          enter-from-class="opacity-0 max-h-0"
          enter-to-class="opacity-100 max-h-96"
          leave-active-class="transition-all duration-200 ease-in"
          leave-from-class="opacity-100 max-h-96"
          leave-to-class="opacity-0 max-h-0"
        >
          <div
            v-if="showReasoning"
            class="mt-2 p-3 bg-gray-100 border border-gray-200 rounded-lg overflow-hidden"
          >
            <div class="text-xs font-medium text-gray-500 mb-1 flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                />
              </svg>
              Internal Reasoning
            </div>
            <div class="text-sm text-gray-700 whitespace-pre-wrap">
              {{ message.internalReasoning }}
            </div>
          </div>
        </transition>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes fade-in {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.animate-fade-in {
  animation: fade-in 0.3s ease-out;
}

.prose {
  line-height: 1.6;
}

.prose code {
  background-color: rgb(229 231 235); /* gray-200 */
  padding: 0.125rem 0.25rem;
  border-radius: 0.25rem;
  font-size: 0.875rem;
}

.prose pre {
  background-color: rgb(31 41 55); /* gray-800 */
  color: rgb(243 244 246); /* gray-100 */
  padding: 0.75rem;
  border-radius: 0.5rem;
  overflow-x: auto;
  margin: 0.5rem 0;
}

.prose pre code {
  background-color: transparent;
  padding: 0;
  color: inherit;
}
</style>
