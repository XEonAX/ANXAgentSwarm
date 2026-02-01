<script setup lang="ts">
/**
 * Skeleton loader component for messages during loading state.
 * Displays an animated placeholder matching the MessageCard layout.
 */
</script>

<template>
  <div class="flex gap-3 py-4 animate-pulse">
    <!-- Avatar Skeleton -->
    <div class="flex-shrink-0 w-10 h-10 rounded-full bg-gray-200"></div>

    <!-- Content Skeleton -->
    <div class="flex-1 min-w-0 max-w-3xl">
      <!-- Header Skeleton -->
      <div class="flex items-center gap-2 mb-2">
        <div class="h-4 w-24 bg-gray-200 rounded"></div>
        <div class="h-4 w-16 bg-gray-100 rounded"></div>
        <div class="h-3 w-12 bg-gray-100 rounded ml-auto"></div>
      </div>

      <!-- Message Body Skeleton -->
      <div class="rounded-lg p-4 bg-gray-100 border border-gray-200 space-y-2">
        <div class="h-4 bg-gray-200 rounded w-full"></div>
        <div class="h-4 bg-gray-200 rounded w-5/6"></div>
        <div class="h-4 bg-gray-200 rounded w-4/6"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.animate-pulse {
  animation: pulse 1.5s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>
