<script setup lang="ts">
import { computed } from 'vue'
import type { SessionDto } from '@/types'
import { SessionStatus } from '@/types'
import { getStatusStyle, getStatusDisplayName, formatTimestamp } from '@/utils/personaStyles'
import SessionListSkeleton from '@/components/SessionListSkeleton.vue'

const props = defineProps<{
  sessions: SessionDto[]
  selectedSessionId?: string | null
  isLoading?: boolean
}>()

const emit = defineEmits<{
  (e: 'select', session: SessionDto): void
  (e: 'create-new'): void
}>()

// Sort sessions by updatedAt (most recent first)
const sortedSessions = computed(() => {
  return [...props.sessions].sort((a, b) => {
    return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  })
})

// Group sessions by date
const groupedSessions = computed(() => {
  const groups: { label: string; sessions: SessionDto[] }[] = []
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const yesterday = new Date(today)
  yesterday.setDate(yesterday.getDate() - 1)
  const lastWeek = new Date(today)
  lastWeek.setDate(lastWeek.getDate() - 7)

  const todaySessions: SessionDto[] = []
  const yesterdaySessions: SessionDto[] = []
  const thisWeekSessions: SessionDto[] = []
  const olderSessions: SessionDto[] = []

  for (const session of sortedSessions.value) {
    const sessionDate = new Date(session.updatedAt)
    sessionDate.setHours(0, 0, 0, 0)

    if (sessionDate.getTime() === today.getTime()) {
      todaySessions.push(session)
    } else if (sessionDate.getTime() === yesterday.getTime()) {
      yesterdaySessions.push(session)
    } else if (sessionDate >= lastWeek) {
      thisWeekSessions.push(session)
    } else {
      olderSessions.push(session)
    }
  }

  if (todaySessions.length > 0) groups.push({ label: 'Today', sessions: todaySessions })
  if (yesterdaySessions.length > 0) groups.push({ label: 'Yesterday', sessions: yesterdaySessions })
  if (thisWeekSessions.length > 0) groups.push({ label: 'This Week', sessions: thisWeekSessions })
  if (olderSessions.length > 0) groups.push({ label: 'Older', sessions: olderSessions })

  return groups
})

function selectSession(session: SessionDto): void {
  emit('select', session)
}

function createNewSession(): void {
  emit('create-new')
}

function getSessionStatusStyle(status: SessionStatus) {
  return getStatusStyle(status)
}

function getSessionStatusName(status: SessionStatus) {
  return getStatusDisplayName(status)
}

function isSelected(sessionId: string): boolean {
  return props.selectedSessionId === sessionId
}

function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength).trim() + '...'
}
</script>

<template>
  <div class="flex flex-col h-full bg-white">
    <!-- Header -->
    <div class="flex-shrink-0 p-4 border-b border-gray-200">
      <div class="flex items-center justify-between mb-3">
        <h2 class="text-lg font-semibold text-gray-900">Sessions</h2>
        <span class="text-sm text-gray-500">{{ sessions.length }} total</span>
      </div>
      
      <!-- New Session Button -->
      <button
        @click="createNewSession"
        class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 font-medium"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 4v16m8-8H4"
          />
        </svg>
        New Session
      </button>
    </div>

    <!-- Session List -->
    <div class="flex-1 overflow-y-auto">
      <!-- Loading State -->
      <div v-if="isLoading" class="p-4">
        <SessionListSkeleton v-for="i in 5" :key="i" />
      </div>

      <!-- Empty State -->
      <div
        v-else-if="sessions.length === 0"
        class="flex flex-col items-center justify-center h-full text-center p-6"
      >
        <div class="text-5xl mb-3">📋</div>
        <h3 class="text-base font-semibold text-gray-700 mb-1">No sessions yet</h3>
        <p class="text-sm text-gray-500">
          Create a new session to get started with AI problem solving.
        </p>
      </div>

      <!-- Session Groups -->
      <div v-else class="py-2">
        <div v-for="group in groupedSessions" :key="group.label" class="mb-4">
          <!-- Group Label -->
          <div class="px-4 py-1 text-xs font-semibold text-gray-500 uppercase tracking-wider">
            {{ group.label }}
          </div>

          <!-- Sessions in Group -->
          <div class="space-y-1 px-2">
            <button
              v-for="session in group.sessions"
              :key="session.id"
              @click="selectSession(session)"
              class="w-full text-left p-3 rounded-lg transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-inset group"
              :class="[
                isSelected(session.id)
                  ? 'bg-primary-50 border border-primary-200'
                  : 'hover:bg-gray-50 border border-transparent'
              ]"
            >
              <!-- Session Header -->
              <div class="flex items-start justify-between gap-2 mb-1">
                <h4
                  class="font-medium text-sm truncate flex-1"
                  :class="isSelected(session.id) ? 'text-primary-900' : 'text-gray-900'"
                >
                  {{ session.title || 'Untitled Session' }}
                </h4>
                
                <!-- Status Badge -->
                <span
                  class="flex-shrink-0 inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium"
                  :class="[
                    getSessionStatusStyle(session.status).bgColor,
                    getSessionStatusStyle(session.status).textColor
                  ]"
                >
                  <span class="mr-0.5">{{ getSessionStatusStyle(session.status).icon }}</span>
                  <span class="hidden sm:inline">{{ getSessionStatusName(session.status) }}</span>
                </span>
              </div>

              <!-- Problem Statement Preview -->
              <p class="text-xs text-gray-500 line-clamp-2 mb-1.5">
                {{ truncateText(session.problemStatement, 100) }}
              </p>

              <!-- Meta Info -->
              <div class="flex items-center justify-between text-xs text-gray-400">
                <span class="flex items-center gap-1">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                    />
                  </svg>
                  {{ session.messageCount }} messages
                </span>
                <span>{{ formatTimestamp(session.updatedAt) }}</span>
              </div>

              <!-- Pulse indicator for active sessions -->
              <div
                v-if="session.status === SessionStatus.Active"
                class="absolute top-3 right-3"
              >
                <span class="relative flex h-2 w-2">
                  <span
                    class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"
                  ></span>
                  <span class="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                </span>
              </div>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
