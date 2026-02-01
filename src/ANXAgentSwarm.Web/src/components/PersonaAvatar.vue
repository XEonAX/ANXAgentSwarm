<script setup lang="ts">
/**
 * PersonaAvatar - Reusable avatar component for displaying persona icons.
 * Provides consistent styling across the application with size variants.
 */
import { computed } from 'vue'
import type { PersonaType } from '@/types'
import { getPersonaStyle, getPersonaDisplayName } from '@/utils/personaStyles'

const props = withDefaults(
  defineProps<{
    /** The persona type to display */
    persona: PersonaType
    /** Size variant: 'xs' | 'sm' | 'md' | 'lg' | 'xl' */
    size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl'
    /** Whether to show pulse animation (for active state) */
    pulse?: boolean
    /** Whether the avatar is clickable */
    clickable?: boolean
    /** Show tooltip with persona name */
    showTooltip?: boolean
  }>(),
  {
    size: 'md',
    pulse: false,
    clickable: false,
    showTooltip: true
  }
)

const emit = defineEmits<{
  (e: 'click', persona: PersonaType): void
}>()

// Computed persona style
const personaStyle = computed(() => getPersonaStyle(props.persona))

// Computed persona name for tooltip
const personaName = computed(() => getPersonaDisplayName(props.persona))

// Size classes mapping
const sizeClasses = computed(() => {
  const sizes = {
    xs: 'w-6 h-6 text-xs',
    sm: 'w-8 h-8 text-sm',
    md: 'w-10 h-10 text-base',
    lg: 'w-12 h-12 text-lg',
    xl: 'w-16 h-16 text-2xl'
  }
  return sizes[props.size]
})

// Icon size based on container size
const iconSize = computed(() => {
  const iconSizes = {
    xs: 'text-xs',
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-xl',
    xl: 'text-2xl'
  }
  return iconSizes[props.size]
})

function handleClick(): void {
  if (props.clickable) {
    emit('click', props.persona)
  }
}
</script>

<template>
  <button
    :class="[
      'relative rounded-full flex items-center justify-center text-white font-bold shadow-md transition-transform',
      `bg-gradient-to-br ${personaStyle.gradient}`,
      sizeClasses,
      clickable ? 'hover:scale-110 cursor-pointer focus:outline-none focus:ring-2 focus:ring-offset-2' : 'cursor-default',
      personaStyle.ringColor
    ]"
    :title="showTooltip ? personaName : undefined"
    @click="handleClick"
    :disabled="!clickable"
    type="button"
  >
    <!-- Icon -->
    <span :class="iconSize">{{ personaStyle.icon }}</span>

    <!-- Pulse Animation -->
    <span
      v-if="pulse"
      class="absolute inset-0 rounded-full animate-ping opacity-30"
      :class="personaStyle.bgColor"
    ></span>
  </button>
</template>

<style scoped>
button:disabled {
  cursor: default;
}
</style>
