<script setup lang="ts">
/**
 * PersonaBadge - Displays persona name with icon and role as a badge.
 * Useful for inline display and lists.
 */
import { computed } from 'vue'
import type { PersonaType } from '@/types'
import { 
  getPersonaStyle, 
  getPersonaDisplayName, 
  getPersonaShortName, 
  getPersonaRole 
} from '@/utils/personaStyles'

const props = withDefaults(
  defineProps<{
    /** The persona type to display */
    persona: PersonaType
    /** Size variant: 'sm' | 'md' | 'lg' */
    size?: 'sm' | 'md' | 'lg'
    /** Whether to show the role subtitle */
    showRole?: boolean
    /** Whether to use compact mode (short name) */
    compact?: boolean
    /** Whether to show icon */
    showIcon?: boolean
  }>(),
  {
    size: 'md',
    showRole: false,
    compact: false,
    showIcon: true
  }
)

// Computed persona style
const personaStyle = computed(() => getPersonaStyle(props.persona))

// Display name (full or short)
const displayName = computed(() => 
  props.compact ? getPersonaShortName(props.persona) : getPersonaDisplayName(props.persona)
)

// Role description
const role = computed(() => getPersonaRole(props.persona))

// Size-based classes
const containerClasses = computed(() => {
  const sizes = {
    sm: 'text-xs px-1.5 py-0.5 gap-1',
    md: 'text-sm px-2 py-1 gap-1.5',
    lg: 'text-base px-3 py-1.5 gap-2'
  }
  return sizes[props.size]
})

const iconClasses = computed(() => {
  const sizes = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base'
  }
  return sizes[props.size]
})
</script>

<template>
  <span
    :class="[
      'inline-flex items-center rounded-full font-medium',
      personaStyle.bgLight,
      personaStyle.borderColor,
      personaStyle.textColor,
      containerClasses,
      'border'
    ]"
  >
    <!-- Icon -->
    <span v-if="showIcon" :class="iconClasses">
      {{ personaStyle.icon }}
    </span>

    <!-- Name and Role -->
    <span class="flex flex-col leading-tight">
      <span class="font-medium">{{ displayName }}</span>
      <span 
        v-if="showRole && !compact" 
        class="text-xs opacity-75"
      >
        {{ role }}
      </span>
    </span>
  </span>
</template>
