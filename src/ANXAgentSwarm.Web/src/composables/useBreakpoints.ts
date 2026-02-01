import { ref, onMounted, onUnmounted, readonly } from 'vue'

/**
 * Breakpoint definitions matching Tailwind CSS defaults.
 */
export const breakpoints = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  '2xl': 1536
} as const

export type BreakpointKey = keyof typeof breakpoints

/**
 * Composable for reactive breakpoint detection.
 * Provides reactive state for current screen size.
 */
export function useBreakpoints() {
  const width = ref(typeof window !== 'undefined' ? window.innerWidth : 0)
  const height = ref(typeof window !== 'undefined' ? window.innerHeight : 0)

  // Current breakpoint name
  const current = ref<BreakpointKey | 'xs'>('xs')

  // Individual breakpoint states
  const isMobile = ref(true)
  const isTablet = ref(false)
  const isDesktop = ref(false)
  const isLargeDesktop = ref(false)

  // Specific breakpoint checks
  const smAndUp = ref(false)
  const mdAndUp = ref(false)
  const lgAndUp = ref(false)
  const xlAndUp = ref(false)
  const xxlAndUp = ref(false)

  function updateBreakpoints(): void {
    width.value = window.innerWidth
    height.value = window.innerHeight

    // Update individual breakpoint states
    smAndUp.value = width.value >= breakpoints.sm
    mdAndUp.value = width.value >= breakpoints.md
    lgAndUp.value = width.value >= breakpoints.lg
    xlAndUp.value = width.value >= breakpoints.xl
    xxlAndUp.value = width.value >= breakpoints['2xl']

    // Update semantic device states
    isMobile.value = !mdAndUp.value
    isTablet.value = mdAndUp.value && !lgAndUp.value
    isDesktop.value = lgAndUp.value && !xlAndUp.value
    isLargeDesktop.value = xlAndUp.value

    // Determine current breakpoint name
    if (xxlAndUp.value) {
      current.value = '2xl'
    } else if (xlAndUp.value) {
      current.value = 'xl'
    } else if (lgAndUp.value) {
      current.value = 'lg'
    } else if (mdAndUp.value) {
      current.value = 'md'
    } else if (smAndUp.value) {
      current.value = 'sm'
    } else {
      current.value = 'xs'
    }
  }

  // Debounced resize handler
  let resizeTimeout: ReturnType<typeof setTimeout> | null = null
  
  function handleResize(): void {
    if (resizeTimeout) {
      clearTimeout(resizeTimeout)
    }
    resizeTimeout = setTimeout(updateBreakpoints, 100)
  }

  onMounted(() => {
    updateBreakpoints()
    window.addEventListener('resize', handleResize)
  })

  onUnmounted(() => {
    window.removeEventListener('resize', handleResize)
    if (resizeTimeout) {
      clearTimeout(resizeTimeout)
    }
  })

  /**
   * Check if current width is at or above a breakpoint.
   */
  function isAtLeast(breakpoint: BreakpointKey): boolean {
    return width.value >= breakpoints[breakpoint]
  }

  /**
   * Check if current width is below a breakpoint.
   */
  function isBelow(breakpoint: BreakpointKey): boolean {
    return width.value < breakpoints[breakpoint]
  }

  /**
   * Check if current width is between two breakpoints.
   */
  function isBetween(minBreakpoint: BreakpointKey, maxBreakpoint: BreakpointKey): boolean {
    return width.value >= breakpoints[minBreakpoint] && width.value < breakpoints[maxBreakpoint]
  }

  return {
    // Dimensions
    width: readonly(width),
    height: readonly(height),
    
    // Current breakpoint
    current: readonly(current),
    
    // Device type states
    isMobile: readonly(isMobile),
    isTablet: readonly(isTablet),
    isDesktop: readonly(isDesktop),
    isLargeDesktop: readonly(isLargeDesktop),
    
    // Breakpoint checks
    smAndUp: readonly(smAndUp),
    mdAndUp: readonly(mdAndUp),
    lgAndUp: readonly(lgAndUp),
    xlAndUp: readonly(xlAndUp),
    xxlAndUp: readonly(xxlAndUp),
    
    // Utility methods
    isAtLeast,
    isBelow,
    isBetween
  }
}
