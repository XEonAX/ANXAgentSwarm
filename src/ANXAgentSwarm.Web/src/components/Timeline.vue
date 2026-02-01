<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import type { MessageDto, PersonaType } from '@/types'
import MessageCard from '@/components/MessageCard.vue'
import MessageSkeleton from '@/components/MessageSkeleton.vue'
import { getPersonaDisplayName, getPersonaStyle } from '@/utils/personaStyles'

const props = defineProps<{
  messages: MessageDto[]
  isLoading?: boolean
  currentPersona?: PersonaType | null
  autoScroll?: boolean
}>()

const emit = defineEmits<{
  (e: 'persona-click', persona: PersonaType): void
}>()

// Ref to the scrollable container
const timelineRef = ref<HTMLElement | null>(null)

// Track if user has manually scrolled up
const userScrolledUp = ref(false)

// Computed for latest message detection
const latestMessageId = computed(() => {
  if (props.messages.length === 0) return null
  return props.messages[props.messages.length - 1]?.id ?? null
})

// Current persona info for the processing indicator
const currentPersonaInfo = computed(() => {
  if (props.currentPersona === null || props.currentPersona === undefined) return null
  return {
    name: getPersonaDisplayName(props.currentPersona),
    style: getPersonaStyle(props.currentPersona)
  }
})

// Scroll to bottom when new messages arrive
watch(
  () => props.messages.length,
  async () => {
    if (props.autoScroll !== false && !userScrolledUp.value) {
      await nextTick()
      scrollToBottom()
    }
  }
)

// Handle scroll events to detect user scrolling up
function handleScroll(): void {
  if (!timelineRef.value) return
  
  const { scrollTop, scrollHeight, clientHeight } = timelineRef.value
  const distanceFromBottom = scrollHeight - scrollTop - clientHeight
  
  // If user scrolled more than 100px from bottom, consider it manual scroll
  userScrolledUp.value = distanceFromBottom > 100
}

// Scroll to the bottom of the timeline
function scrollToBottom(): void {
  if (timelineRef.value) {
    timelineRef.value.scrollTop = timelineRef.value.scrollHeight
    userScrolledUp.value = false
  }
}

// Handle persona click from MessageCard
function handlePersonaClick(persona: PersonaType): void {
  emit('persona-click', persona)
}

// Scroll to bottom on mount
onMounted(() => {
  scrollToBottom()
})

// Expose scrollToBottom for parent components
defineExpose({
  scrollToBottom
})
</script>

<template>
  <div class="relative flex flex-col h-full">
    <!-- Timeline Container -->
    <div
      ref="timelineRef"
      class="flex-1 overflow-y-auto px-4 py-2"
      @scroll="handleScroll"
    >
      <!-- Empty State -->
      <div
        v-if="!isLoading && messages.length === 0"
        class="flex flex-col items-center justify-center h-full text-center py-12"
      >
        <div class="text-6xl mb-4">💬</div>
        <h3 class="text-lg font-semibold text-gray-700 mb-2">No messages yet</h3>
        <p class="text-gray-500 max-w-sm">
          Submit a problem statement to start the AI agents working on your request.
        </p>
      </div>

      <!-- Loading Skeletons -->
      <div v-else-if="isLoading && messages.length === 0" class="space-y-4 py-4">
        <MessageSkeleton v-for="i in 3" :key="i" />
      </div>

      <!-- Messages List -->
      <div v-else class="space-y-1">
        <TransitionGroup name="message-list">
          <MessageCard
            v-for="message in messages"
            :key="message.id"
            :message="message"
            :is-latest="message.id === latestMessageId"
            @persona-click="handlePersonaClick"
          />
        </TransitionGroup>

        <!-- Processing Indicator -->
        <div
          v-if="currentPersonaInfo && !isLoading"
          class="flex items-center gap-3 py-4 animate-fade-in"
        >
          <!-- Animated Avatar -->
          <div
            class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center text-white shadow-md animate-pulse"
            :class="`bg-gradient-to-br ${currentPersonaInfo.style.gradient}`"
          >
            <span class="text-lg">{{ currentPersonaInfo.style.icon }}</span>
          </div>

          <!-- Typing Indicator -->
          <div
            class="px-4 py-3 rounded-lg border"
            :class="[currentPersonaInfo.style.bgLight, currentPersonaInfo.style.borderColor]"
          >
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium" :class="currentPersonaInfo.style.textColor">
                {{ currentPersonaInfo.name }} is thinking...
              </span>
              <div class="flex gap-1">
                <span
                  v-for="i in 3"
                  :key="i"
                  class="w-1.5 h-1.5 rounded-full animate-bounce"
                  :class="currentPersonaInfo.style.bgColor"
                  :style="{ animationDelay: `${i * 0.15}s` }"
                ></span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Scroll to Bottom Button -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 translate-y-2"
    >
      <button
        v-if="userScrolledUp && messages.length > 0"
        @click="scrollToBottom"
        class="absolute bottom-4 right-4 p-2 bg-white border border-gray-300 rounded-full shadow-lg hover:bg-gray-50 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
        title="Scroll to bottom"
      >
        <svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M19 14l-7 7m0 0l-7-7m7 7V3"
          />
        </svg>
      </button>
    </Transition>
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

/* Message list transition */
.message-list-enter-active {
  transition: all 0.3s ease-out;
}

.message-list-leave-active {
  transition: all 0.2s ease-in;
}

.message-list-enter-from {
  opacity: 0;
  transform: translateY(20px);
}

.message-list-leave-to {
  opacity: 0;
  transform: translateX(-20px);
}

.message-list-move {
  transition: transform 0.3s ease;
}
</style>
