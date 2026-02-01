<script setup lang="ts">
/**
 * Skeleton loader component for session list items during loading state.
 */
</script>

<template>
  <div class="p-3 rounded-lg border border-gray-100 mb-2 animate-pulse">
    <!-- Header Skeleton -->
    <div class="flex items-start justify-between gap-2 mb-2">
      <div class="h-4 bg-gray-200 rounded w-3/4"></div>
      <div class="h-5 w-16 bg-gray-100 rounded"></div>
    </div>

    <!-- Description Skeleton -->
    <div class="space-y-1.5 mb-2">
      <div class="h-3 bg-gray-100 rounded w-full"></div>
      <div class="h-3 bg-gray-100 rounded w-2/3"></div>
    </div>

    <!-- Meta Skeleton -->
    <div class="flex items-center justify-between">
      <div class="h-3 w-20 bg-gray-100 rounded"></div>
      <div class="h-3 w-16 bg-gray-100 rounded"></div>
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
