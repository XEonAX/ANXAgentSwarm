<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSessionStore } from '@/stores/sessionStore'
import { useSignalR } from '@/composables/useSignalR'

const sessionStore = useSessionStore()
const signalR = useSignalR()

const isInitialized = ref(false)
const initError = ref<string | null>(null)

function reload(): void {
  globalThis.location.reload()
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
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header class="bg-white shadow-sm border-b border-gray-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <div class="flex items-center">
            <span class="text-2xl mr-2">🤖</span>
            <h1 class="text-xl font-bold text-gray-900">ANXAgentSwarm</h1>
          </div>
          <div class="flex items-center space-x-4">
            <span 
              class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
              :class="signalR.connectionState.value === 'connected' 
                ? 'bg-green-100 text-green-800' 
                : 'bg-yellow-100 text-yellow-800'"
            >
              <span 
                class="w-2 h-2 mr-1.5 rounded-full"
                :class="signalR.connectionState.value === 'connected' 
                  ? 'bg-green-400' 
                  : 'bg-yellow-400'"
              ></span>
              {{ signalR.connectionState.value === 'connected' ? 'Connected' : 'Connecting...' }}
            </span>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Loading State -->
      <div v-if="!isInitialized && !initError" class="flex justify-center items-center h-64">
        <div class="text-center">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
          <p class="mt-4 text-gray-600">Initializing...</p>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="initError" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
        <p class="text-red-800 font-medium">{{ initError }}</p>
        <button 
          @click="reload"
          class="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
        >
          Retry
        </button>
      </div>

      <!-- Ready State -->
      <div v-else class="space-y-6">
        <!-- Welcome Message -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h2 class="text-2xl font-bold text-gray-900 mb-2">
            Welcome to ANXAgentSwarm
          </h2>
          <p class="text-gray-600">
            An Ollama-based multi-agent system that orchestrates AI personas to collaboratively solve problems.
          </p>
        </div>

        <!-- Sessions List Placeholder -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Sessions</h3>
          <div v-if="sessionStore.sessions.length === 0" class="text-center py-8 text-gray-500">
            <p>No sessions yet. Create a new session to get started!</p>
          </div>
          <div v-else class="space-y-2">
            <div 
              v-for="session in sessionStore.sessions" 
              :key="session.id"
              class="p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
            >
              <div class="flex justify-between items-start">
                <div>
                  <h4 class="font-medium text-gray-900">{{ session.title }}</h4>
                  <p class="text-sm text-gray-500 mt-1 line-clamp-2">{{ session.problemStatement }}</p>
                </div>
                <span 
                  class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                  :class="{
                    'bg-green-100 text-green-800': session.status === 0,
                    'bg-yellow-100 text-yellow-800': session.status === 1,
                    'bg-blue-100 text-blue-800': session.status === 2,
                    'bg-orange-100 text-orange-800': session.status === 3,
                    'bg-gray-100 text-gray-800': session.status === 4,
                    'bg-red-100 text-red-800': session.status === 5
                  }"
                >
                  {{ ['Active', 'Waiting', 'Completed', 'Stuck', 'Cancelled', 'Error'][session.status] }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- Footer -->
    <footer class="border-t border-gray-200 bg-white mt-auto">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <p class="text-center text-sm text-gray-500">
          ANXAgentSwarm - Multi-Agent Problem Solving System
        </p>
      </div>
    </footer>
  </div>
</template>
