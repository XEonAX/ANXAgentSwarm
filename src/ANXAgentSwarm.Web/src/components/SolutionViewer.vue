<script setup lang="ts">
/**
 * SolutionViewer - Component for displaying final solutions with markdown formatting.
 * Provides copy functionality and expandable view.
 */
import { ref, computed } from 'vue'

const props = defineProps<{
  /** The solution content to display */
  solution: string
  /** Optional title for the solution */
  title?: string
  /** Whether the viewer starts collapsed */
  startCollapsed?: boolean
}>()

const emit = defineEmits<{
  (e: 'copy'): void
}>()

// State
const isExpanded = ref(!props.startCollapsed)
const isCopied = ref(false)

// Computed preview (first 500 chars if collapsed)
const previewContent = computed(() => {
  if (isExpanded.value || props.solution.length <= 500) {
    return props.solution
  }
  return props.solution.substring(0, 500) + '...'
})

// Check if content is long enough to need expand/collapse
const isLongContent = computed(() => props.solution.length > 500)

// Simple markdown-like formatting
const formattedContent = computed(() => {
  let content = previewContent.value

  // Escape HTML
  content = content
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')

  // Code blocks (```...```)
  content = content.replace(
    /```(\w*)\n([\s\S]*?)```/g,
    '<pre class="bg-gray-800 text-gray-100 p-4 rounded-lg overflow-x-auto my-3 text-sm"><code>$2</code></pre>'
  )

  // Inline code (`...`)
  content = content.replace(
    /`([^`]+)`/g,
    '<code class="bg-gray-200 px-1.5 py-0.5 rounded text-sm font-mono text-gray-800">$1</code>'
  )

  // Bold (**...** or __...__) 
  content = content.replace(/\*\*([^*]+)\*\*/g, '<strong class="font-semibold">$1</strong>')
  content = content.replace(/__([^_]+)__/g, '<strong class="font-semibold">$1</strong>')

  // Italic (*...* or _..._)
  content = content.replace(/\*([^*]+)\*/g, '<em class="italic">$1</em>')
  content = content.replace(/_([^_]+)_/g, '<em class="italic">$1</em>')

  // Headers (# ## ###)
  content = content.replace(/^### (.+)$/gm, '<h3 class="text-base font-semibold mt-4 mb-2">$1</h3>')
  content = content.replace(/^## (.+)$/gm, '<h2 class="text-lg font-semibold mt-4 mb-2">$1</h2>')
  content = content.replace(/^# (.+)$/gm, '<h1 class="text-xl font-bold mt-4 mb-2">$1</h1>')

  // Bullet lists
  content = content.replace(/^- (.+)$/gm, '<li class="ml-4 list-disc">$1</li>')
  content = content.replace(/^• (.+)$/gm, '<li class="ml-4 list-disc">$1</li>')

  // Numbered lists
  content = content.replace(/^\d+\. (.+)$/gm, '<li class="ml-4 list-decimal">$1</li>')

  // Line breaks
  content = content.replace(/\n/g, '<br>')

  return content
})

// Copy solution to clipboard
async function copyToClipboard(): Promise<void> {
  try {
    await navigator.clipboard.writeText(props.solution)
    isCopied.value = true
    emit('copy')
    
    // Reset after 2 seconds
    setTimeout(() => {
      isCopied.value = false
    }, 2000)
  } catch (error) {
    console.error('Failed to copy to clipboard:', error)
  }
}

// Toggle expanded state
function toggleExpanded(): void {
  isExpanded.value = !isExpanded.value
}
</script>

<template>
  <div class="bg-emerald-50 border border-emerald-200 rounded-xl overflow-hidden">
    <!-- Header -->
    <div class="px-4 py-3 bg-emerald-100 border-b border-emerald-200 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <span class="text-2xl">✅</span>
        <h3 class="font-semibold text-emerald-800">
          {{ title || 'Solution' }}
        </h3>
      </div>

      <!-- Actions -->
      <div class="flex items-center gap-2">
        <!-- Copy Button -->
        <button
          @click="copyToClipboard"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-emerald-700 hover:text-emerald-900 hover:bg-emerald-200 rounded-lg transition-colors"
          :title="isCopied ? 'Copied!' : 'Copy to clipboard'"
        >
          <svg
            v-if="!isCopied"
            class="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"
            />
          </svg>
          <svg
            v-else
            class="w-4 h-4 text-emerald-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M5 13l4 4L19 7"
            />
          </svg>
          <span>{{ isCopied ? 'Copied!' : 'Copy' }}</span>
        </button>

        <!-- Expand/Collapse Button -->
        <button
          v-if="isLongContent"
          @click="toggleExpanded"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-emerald-700 hover:text-emerald-900 hover:bg-emerald-200 rounded-lg transition-colors"
        >
          <svg
            class="w-4 h-4 transition-transform"
            :class="isExpanded ? 'rotate-180' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M19 9l-7 7-7-7"
            />
          </svg>
          <span>{{ isExpanded ? 'Collapse' : 'Expand' }}</span>
        </button>
      </div>
    </div>

    <!-- Content -->
    <div class="px-4 py-4">
      <div
        class="prose-solution text-gray-800 leading-relaxed"
        v-html="formattedContent"
      ></div>

      <!-- Show More Indicator -->
      <div
        v-if="!isExpanded && isLongContent"
        class="mt-4 text-center"
      >
        <button
          @click="toggleExpanded"
          class="inline-flex items-center gap-1 text-sm text-emerald-600 hover:text-emerald-700 font-medium"
        >
          <span>Show full solution</span>
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Footer Meta -->
    <div class="px-4 py-2 bg-emerald-100/50 border-t border-emerald-200 text-sm text-emerald-600">
      <span>{{ solution.length.toLocaleString() }} characters</span>
    </div>
  </div>
</template>

<style scoped>
.prose-solution {
  line-height: 1.7;
}

.prose-solution :deep(pre) {
  white-space: pre-wrap;
  word-wrap: break-word;
}

.prose-solution :deep(li) {
  margin-bottom: 0.25rem;
}

.prose-solution :deep(br + br) {
  display: none;
}
</style>
