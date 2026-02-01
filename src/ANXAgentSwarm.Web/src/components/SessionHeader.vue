<script setup lang="ts">
import { computed } from 'vue'
import type { PersonaType, SessionStatus } from '@/types'
import { SessionStatus as SessionStatusEnum } from '@/types'
import {
  getStatusStyle,
  getPersonaStyle,
  getPersonaDisplayName
} from '@/utils/personaStyles'

const props = defineProps<{
  title: string
  status: SessionStatus | null
  statusText: string
  currentPersona: PersonaType | null
  isLoading?: boolean
}>()

const emit = defineEmits<{
  (e: 'back'): void
  (e: 'cancel'): void
}>()

// Computed status style
const statusStyle = computed(() => {
  if (props.status === null) return null
  return getStatusStyle(props.status)
})

// Computed persona info
const personaInfo = computed(() => {
  if (props.currentPersona === null || props.currentPersona === undefined) return null
  return {
    name: getPersonaDisplayName(props.currentPersona),
    style: getPersonaStyle(props.currentPersona)
  }
})

// Whether session can be cancelled
const canCancel = computed(() => {
  return props.status === SessionStatusEnum.Active || 
         props.status === SessionStatusEnum.WaitingForClarification
})

// Status shows pulse animation
const showPulse = computed(() => {
  return props.status === SessionStatusEnum.Active
})

function handleBack(): void {
  emit('back')
}

function handleCancel(): void {
  emit('cancel')
}
</script>

<template>
  <header class="flex-shrink-0 bg-white border-b border-gray-200">
    <!-- Main Header Row -->
    <div class="px-4 py-3 flex items-center gap-4">
      <!-- Back Button -->
      <button
        @click="handleBack"
        class="flex-shrink-0 p-2 -ml-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500"
        title="Back to sessions"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 19l-7-7 7-7"
          />
        </svg>
      </button>

      <!-- Title and Status -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <h1 class="text-lg font-semibold text-gray-900 truncate">
            {{ title || 'Session' }}
          </h1>
          
          <!-- Status Badge -->
          <span
            v-if="statusStyle"
            class="flex-shrink-0 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
            :class="[statusStyle.bgColor, statusStyle.textColor]"
          >
            <!-- Pulse indicator for active status -->
            <span v-if="showPulse" class="relative flex h-2 w-2 mr-1.5">
              <span
                class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
                :class="statusStyle.pulseColor"
              ></span>
              <span
                class="relative inline-flex rounded-full h-2 w-2"
                :class="statusStyle.pulseColor"
              ></span>
            </span>
            <span v-else class="mr-1">{{ statusStyle.icon }}</span>
            {{ statusText }}
          </span>
        </div>

        <!-- Current Persona Indicator -->
        <div v-if="personaInfo && status === SessionStatusEnum.Active" class="mt-1">
          <span class="inline-flex items-center text-sm">
            <span
              class="w-5 h-5 rounded-full flex items-center justify-center text-white text-xs mr-1.5"
              :class="`bg-gradient-to-br ${personaInfo.style.gradient}`"
            >
              {{ personaInfo.style.icon }}
            </span>
            <span class="text-gray-500">
              <span class="font-medium" :class="personaInfo.style.textColor">
                {{ personaInfo.name }}
              </span>
              is working...
            </span>
          </span>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex-shrink-0 flex items-center gap-2">
        <!-- Loading Indicator -->
        <div
          v-if="isLoading"
          class="w-5 h-5 border-2 border-primary-500 border-t-transparent rounded-full animate-spin"
        ></div>

        <!-- Cancel Button -->
        <button
          v-if="canCancel"
          @click="handleCancel"
          class="px-3 py-1.5 text-sm text-red-600 hover:text-red-700 hover:bg-red-50 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2"
        >
          Cancel
        </button>

        <!-- More Actions Menu (placeholder for future) -->
        <button
          class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500"
          title="More actions"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z"
            />
          </svg>
        </button>
      </div>
    </div>
  </header>
</template>

<style scoped>
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.animate-spin {
  animation: spin 1s linear infinite;
}
</style>
