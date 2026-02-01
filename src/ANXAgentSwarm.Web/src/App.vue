<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useSessionStore } from '@/stores/sessionStore'
import { useSignalR } from '@/composables/useSignalR'
import SessionList from '@/components/SessionList.vue'
import SessionView from '@/components/SessionView.vue'
import type { SessionDto } from '@/types'

const sessionStore = useSessionStore()
const signalR = useSignalR()

const isInitialized = ref(false)
const initError = ref<string | null>(null)

// Currently selected session ID
const selectedSessionId = ref<string | null>(null)

// View state: 'list' | 'session' | 'new'
const currentView = ref<'list' | 'session' | 'new'>('list')

// Connection status
const connectionStatus = computed(() => {
  const state = signalR.connectionState.value
  return {
    isConnected: state === 'connected',
    text: state === 'connected' ? 'Connected' : 
          state === 'connecting' ? 'Connecting...' :
          state === 'reconnecting' ? 'Reconnecting...' : 'Disconnected',
    color: state === 'connected' ? 'green' : 
           state === 'error' ? 'red' : 'yellow'
  }
})

function reload(): void {
  globalThis.location.reload()
}

// Handle session selection from list
function handleSelectSession(session: SessionDto): void {
  selectedSessionId.value = session.id
  currentView.value = 'session'
}

// Handle create new session
function handleCreateNew(): void {
  selectedSessionId.value = null
  currentView.value = 'new'
}

// Handle back to list
function handleBack(): void {
  currentView.value = 'list'
  selectedSessionId.value = null
}

// Handle session created
function handleSessionCreated(sessionId: string): void {
  selectedSessionId.value = sessionId
  currentView.value = 'session'
  // Refresh sessions list
  sessionStore.fetchSessions()
}

onMounted(async () => {
  try {
    // Connect to SignalR
    await signalR.connect()
    
    // Load existing sessions
    await sessionStore.fetchSessions()
    
    isInitialized.value = true
  } catch (error) {
    initError.value = error instanceof Error ? error.message : 'Failed to initialize'
    console.error('Failed to initialize app:', error)
  }
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex flex-col">
    <!-- Header -->
    <header class="flex-shrink-0 bg-white shadow-sm border-b border-gray-200 z-10">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-14">
          <div class="flex items-center">
            <button 
              @click="handleBack"
              class="flex items-center hover:opacity-80 transition-opacity"
            >
              <span class="text-2xl mr-2">🤖</span>
              <h1 class="text-xl font-bold text-gray-900">ANXAgentSwarm</h1>
            </button>
          </div>
          <div class="flex items-center space-x-4">
            <!-- Connection Status -->
            <span 
              class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
              :class="{
                'bg-green-100 text-green-800': connectionStatus.color === 'green',
                'bg-yellow-100 text-yellow-800': connectionStatus.color === 'yellow',
                'bg-red-100 text-red-800': connectionStatus.color === 'red'
              }"
            >
              <span 
                class="w-2 h-2 mr-1.5 rounded-full"
                :class="{
                  'bg-green-400': connectionStatus.color === 'green',
                  'bg-yellow-400': connectionStatus.color === 'yellow',
                  'bg-red-400': connectionStatus.color === 'red'
                }"
              ></span>
              {{ connectionStatus.text }}
            </span>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="flex-1 flex overflow-hidden">
      <!-- Loading State -->
      <div v-if="!isInitialized && !initError" class="flex-1 flex justify-center items-center">
        <div class="text-center">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
          <p class="mt-4 text-gray-600">Initializing...</p>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="initError" class="flex-1 flex items-center justify-center p-8">
        <div class="bg-red-50 border border-red-200 rounded-lg p-6 text-center max-w-md">
          <span class="text-4xl mb-4 block">❌</span>
          <p class="text-red-800 font-medium mb-2">Failed to Initialize</p>
          <p class="text-red-600 text-sm mb-4">{{ initError }}</p>
          <button 
            @click="reload"
            class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>

      <!-- App Layout -->
      <template v-else>
        <!-- Mobile: Show either list or session -->
        <div class="flex-1 flex lg:hidden">
          <!-- Session List (Mobile) -->
          <Transition
            enter-active-class="transition-all duration-200 ease-out"
            enter-from-class="opacity-0 -translate-x-full"
            enter-to-class="opacity-100 translate-x-0"
            leave-active-class="transition-all duration-200 ease-in"
            leave-from-class="opacity-100 translate-x-0"
            leave-to-class="opacity-0 -translate-x-full"
          >
            <div v-if="currentView === 'list'" class="w-full">
              <SessionList
                :sessions="sessionStore.sessions"
                :selected-session-id="selectedSessionId"
                :is-loading="sessionStore.isLoading"
                @select="handleSelectSession"
                @create-new="handleCreateNew"
              />
            </div>
          </Transition>

          <!-- Session View (Mobile) -->
          <Transition
            enter-active-class="transition-all duration-200 ease-out"
            enter-from-class="opacity-0 translate-x-full"
            enter-to-class="opacity-100 translate-x-0"
            leave-active-class="transition-all duration-200 ease-in"
            leave-from-class="opacity-100 translate-x-0"
            leave-to-class="opacity-0 translate-x-full"
          >
            <div 
              v-if="currentView === 'session' || currentView === 'new'" 
              class="w-full"
            >
              <SessionView
                :session-id="selectedSessionId"
                @back="handleBack"
                @session-created="handleSessionCreated"
              />
            </div>
          </Transition>
        </div>

        <!-- Desktop: Split view -->
        <div class="hidden lg:flex flex-1">
          <!-- Sidebar - Session List -->
          <aside 
            class="w-80 xl:w-96 flex-shrink-0 border-r border-gray-200 bg-white overflow-hidden"
          >
            <SessionList
              :sessions="sessionStore.sessions"
              :selected-session-id="selectedSessionId"
              :is-loading="sessionStore.isLoading"
              @select="handleSelectSession"
              @create-new="handleCreateNew"
            />
          </aside>

          <!-- Main Content Area -->
          <div class="flex-1 flex flex-col min-w-0">
            <!-- No Session Selected -->
            <div 
              v-if="currentView === 'list' && !selectedSessionId"
              class="flex-1 flex flex-col items-center justify-center p-8 text-center"
            >
              <div class="text-6xl mb-4">🤖</div>
              <h2 class="text-2xl font-bold text-gray-900 mb-2">
                Welcome to ANXAgentSwarm
              </h2>
              <p class="text-gray-600 max-w-md mb-6">
                An Ollama-based multi-agent system that orchestrates AI personas 
                to collaboratively solve problems through sequential Q&A interactions.
              </p>
              <button
                @click="handleCreateNew"
                class="inline-flex items-center gap-2 px-6 py-3 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors font-medium"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Start New Session
              </button>
            </div>

            <!-- Session View -->
            <SessionView
              v-else
              :session-id="selectedSessionId"
              @back="handleBack"
              @session-created="handleSessionCreated"
            />
          </div>
        </div>
      </template>
    </main>

    <!-- Footer -->
    <footer class="flex-shrink-0 border-t border-gray-200 bg-white">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
        <p class="text-center text-sm text-gray-500">
          ANXAgentSwarm - Multi-Agent Problem Solving System
        </p>
      </div>
    </footer>
  </div>
</template>
