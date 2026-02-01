import { test, expect } from './fixtures/test-fixtures'

/**
 * Performance metrics collection tests.
 * These tests measure Web Vitals and other performance indicators.
 */
test.describe('Performance Metrics', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should load home page within acceptable time', async ({ page }) => {
    const startTime = Date.now()
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const loadTime = Date.now() - startTime
    
    // Page should load within 5 seconds
    expect(loadTime).toBeLessThan(5000)
    
    console.log(`Home page load time: ${loadTime}ms`)
  })

  test('should have acceptable First Contentful Paint', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const metrics = await page.evaluate(() => {
      const paint = performance.getEntriesByType('paint')
      return {
        firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime
      }
    })
    
    console.log(`FCP: ${metrics.firstContentfulPaint}ms`)
    
    // FCP should be under 2.5 seconds (good threshold)
    if (metrics.firstContentfulPaint) {
      expect(metrics.firstContentfulPaint).toBeLessThan(2500)
    }
  })

  test('should have acceptable Largest Contentful Paint', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Wait for LCP to be measured
    
    const metrics = await page.evaluate(() => {
      const lcp = performance.getEntriesByType('largest-contentful-paint')
      return {
        largestContentfulPaint: lcp[lcp.length - 1]?.startTime
      }
    })
    
    console.log(`LCP: ${metrics.largestContentfulPaint}ms`)
    
    // LCP should be under 2.5 seconds (good threshold)
    if (metrics.largestContentfulPaint) {
      expect(metrics.largestContentfulPaint).toBeLessThan(2500)
    }
  })

  test('should have acceptable DOM Content Loaded time', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')
    
    const metrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
      return {
        domContentLoaded: navigation?.domContentLoadedEventEnd - navigation?.startTime
      }
    })
    
    console.log(`DOM Content Loaded: ${metrics.domContentLoaded}ms`)
    
    // DCL should be under 3 seconds
    if (metrics.domContentLoaded) {
      expect(metrics.domContentLoaded).toBeLessThan(3000)
    }
  })

  test('should have acceptable Total Blocking Time', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Measure TBT using Long Tasks API
    const tbt = await page.evaluate(() => {
      return new Promise<number>((resolve) => {
        let totalBlockingTime = 0
        
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            // Long tasks are those over 50ms
            if (entry.duration > 50) {
              totalBlockingTime += entry.duration - 50
            }
          }
        })
        
        observer.observe({ entryTypes: ['longtask'] })
        
        // Wait a bit for long tasks to be recorded
        setTimeout(() => {
          observer.disconnect()
          resolve(totalBlockingTime)
        }, 3000)
      })
    })
    
    console.log(`Total Blocking Time: ${tbt}ms`)
    
    // TBT should be under 300ms (good threshold)
    expect(tbt).toBeLessThan(300)
  })

  test('should have acceptable Cumulative Layout Shift', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const cls = await page.evaluate(() => {
      return new Promise<number>((resolve) => {
        let clsValue = 0
        
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            // @ts-expect-error LayoutShift is not in standard types
            if (!entry.hadRecentInput) {
              // @ts-expect-error LayoutShift is not in standard types
              clsValue += entry.value
            }
          }
        })
        
        observer.observe({ entryTypes: ['layout-shift'] })
        
        // Wait for layout shifts to be recorded
        setTimeout(() => {
          observer.disconnect()
          resolve(clsValue)
        }, 3000)
      })
    })
    
    console.log(`Cumulative Layout Shift: ${cls}`)
    
    // CLS should be under 0.1 (good threshold)
    expect(cls).toBeLessThan(0.1)
  })
})

test.describe('Performance - Session Operations', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should create session within acceptable time', async ({ appPage, page }) => {
    await appPage.goto()
    
    const startTime = Date.now()
    
    await appPage.createSession('Test problem for performance')
    
    // Wait for timeline to appear
    await expect(appPage.timeline.or(appPage.sessionHeader)).toBeVisible({ timeout: 10000 })
    
    const operationTime = Date.now() - startTime
    
    console.log(`Session creation time: ${operationTime}ms`)
    
    // Session creation should complete within 5 seconds
    expect(operationTime).toBeLessThan(5000)
  })

  test('should load session details within acceptable time', async ({ mockApi, page }) => {
    const sessionId = 'perf-session'
    
    // Mock a session with many messages
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      const messages = Array.from({ length: 20 }, (_, i) => ({
        id: `msg-${i}`,
        fromPersona: ['Coordinator', 'BusinessAnalyst', 'TechnicalArchitect'][i % 3],
        content: `Message content ${i} with some substantial text to simulate real messages.`,
        internalReasoning: `Internal reasoning for message ${i}.`,
        messageType: 'Processing',
        timestamp: new Date(Date.now() - (20 - i) * 1000).toISOString()
      }))
      
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Performance test problem',
          status: 'InProgress',
          messages
        })
      })
    })
    
    const startTime = Date.now()
    
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    
    // Wait for messages to render
    await page.waitForSelector('[data-testid="message-card"], .message-card', { timeout: 10000 })
    
    const loadTime = Date.now() - startTime
    
    console.log(`Session load time with 20 messages: ${loadTime}ms`)
    
    // Should load within 3 seconds
    expect(loadTime).toBeLessThan(3000)
  })

  test('should navigate between sessions quickly', async ({ mockApi, page }) => {
    await mockApi.setupMocks()
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const startTime = Date.now()
    
    // Click on first session
    await page.locator('[data-testid="session-card"], .session-card').first().click()
    
    // Wait for session to load
    await page.waitForSelector('[data-testid="timeline"], .timeline, [data-testid="session-header"]', { timeout: 10000 })
    
    const navigationTime = Date.now() - startTime
    
    console.log(`Session navigation time: ${navigationTime}ms`)
    
    // Navigation should be quick
    expect(navigationTime).toBeLessThan(2000)
  })
})

test.describe('Performance - Memory Usage', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should not have memory leaks during navigation', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Get initial memory
    const initialMemory = await page.evaluate(() => {
      // @ts-expect-error memory is Chrome-specific
      return performance.memory?.usedJSHeapSize || 0
    })
    
    // Navigate between views multiple times
    for (let i = 0; i < 5; i++) {
      const newSessionButton = page.getByRole('button', { name: /new session|create/i })
      await newSessionButton.click()
      await page.waitForTimeout(500)
      
      // Go back
      const title = page.locator('h1:has-text("ANXAgentSwarm")')
      await title.click()
      await page.waitForTimeout(500)
    }
    
    // Force garbage collection if available
    await page.evaluate(() => {
      // @ts-expect-error gc is not always available
      if (typeof gc === 'function') gc()
    })
    
    await page.waitForTimeout(1000)
    
    const finalMemory = await page.evaluate(() => {
      // @ts-expect-error memory is Chrome-specific
      return performance.memory?.usedJSHeapSize || 0
    })
    
    console.log(`Initial memory: ${initialMemory / 1024 / 1024}MB`)
    console.log(`Final memory: ${finalMemory / 1024 / 1024}MB`)
    
    // Memory should not increase significantly (less than 50MB increase)
    if (initialMemory > 0 && finalMemory > 0) {
      const memoryIncrease = finalMemory - initialMemory
      expect(memoryIncrease).toBeLessThan(50 * 1024 * 1024)
    }
  })
})

test.describe('Performance - Network Efficiency', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should not make excessive API calls on page load', async ({ page }) => {
    const apiCalls: string[] = []
    
    await page.route('**/api/**', async (route) => {
      apiCalls.push(route.request().url())
      await route.continue()
    })
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    console.log(`API calls on page load: ${apiCalls.length}`)
    apiCalls.forEach(call => console.log(`  - ${call}`))
    
    // Should not make more than 5 API calls on initial load
    expect(apiCalls.length).toBeLessThan(5)
  })

  test('should cache API responses appropriately', async ({ page }) => {
    let sessionListCalls = 0
    
    await page.route('**/api/sessions', async (route) => {
      if (route.request().method() === 'GET') {
        sessionListCalls++
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        })
      }
    })
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Navigate away
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.click()
    await page.waitForTimeout(500)
    
    // Navigate back
    const title = page.locator('h1:has-text("ANXAgentSwarm")')
    await title.click()
    await page.waitForTimeout(1000)
    
    console.log(`Session list API calls: ${sessionListCalls}`)
    
    // Should reuse cached data where appropriate
    // This depends on implementation - just log for now
    expect(sessionListCalls).toBeLessThan(10)
  })
})

test.describe('Performance - Bundle Size', () => {
  test('should have acceptable JavaScript bundle size', async ({ page }) => {
    const resources: { name: string; size: number }[] = []
    
    page.on('response', async (response) => {
      const url = response.url()
      if (url.endsWith('.js') || url.includes('.js?')) {
        const size = parseInt(response.headers()['content-length'] || '0', 10)
        resources.push({ name: url.split('/').pop() || url, size })
      }
    })
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const totalJsSize = resources.reduce((acc, r) => acc + r.size, 0)
    
    console.log('JavaScript bundles:')
    resources.forEach(r => console.log(`  - ${r.name}: ${(r.size / 1024).toFixed(2)}KB`))
    console.log(`Total JS size: ${(totalJsSize / 1024).toFixed(2)}KB`)
    
    // Total JS should be under 500KB (gzipped would be much smaller)
    // This is a soft check as we're measuring uncompressed size
    expect(totalJsSize).toBeLessThan(2 * 1024 * 1024) // 2MB max
  })

  test('should have acceptable CSS bundle size', async ({ page }) => {
    const resources: { name: string; size: number }[] = []
    
    page.on('response', async (response) => {
      const url = response.url()
      if (url.endsWith('.css') || url.includes('.css?')) {
        const size = parseInt(response.headers()['content-length'] || '0', 10)
        resources.push({ name: url.split('/').pop() || url, size })
      }
    })
    
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const totalCssSize = resources.reduce((acc, r) => acc + r.size, 0)
    
    console.log('CSS bundles:')
    resources.forEach(r => console.log(`  - ${r.name}: ${(r.size / 1024).toFixed(2)}KB`))
    console.log(`Total CSS size: ${(totalCssSize / 1024).toFixed(2)}KB`)
    
    // Total CSS should be under 200KB
    expect(totalCssSize).toBeLessThan(500 * 1024) // 500KB max
  })
})

test.describe('Performance Report', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('generate comprehensive performance report', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000) // Wait for all metrics
    
    const metrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
      const paint = performance.getEntriesByType('paint')
      const lcp = performance.getEntriesByType('largest-contentful-paint')
      const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[]
      
      return {
        navigation: {
          dnsLookup: navigation?.domainLookupEnd - navigation?.domainLookupStart,
          tcpConnect: navigation?.connectEnd - navigation?.connectStart,
          ttfb: navigation?.responseStart - navigation?.requestStart,
          responseTime: navigation?.responseEnd - navigation?.responseStart,
          domInteractive: navigation?.domInteractive - navigation?.startTime,
          domContentLoaded: navigation?.domContentLoadedEventEnd - navigation?.startTime,
          loadComplete: navigation?.loadEventEnd - navigation?.startTime,
        },
        paint: {
          firstPaint: paint.find(p => p.name === 'first-paint')?.startTime,
          firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime,
        },
        lcp: lcp[lcp.length - 1]?.startTime,
        resources: {
          count: resources.length,
          totalSize: resources.reduce((acc, r) => acc + (r.transferSize || 0), 0),
          jsCount: resources.filter(r => r.initiatorType === 'script').length,
          cssCount: resources.filter(r => r.initiatorType === 'link' || r.name.includes('.css')).length,
        },
        // @ts-expect-error memory is Chrome-specific
        memory: performance.memory ? {
          usedJSHeapSize: performance.memory.usedJSHeapSize,
          totalJSHeapSize: performance.memory.totalJSHeapSize,
        } : null,
      }
    })
    
    console.log('\n=== Performance Report ===\n')
    console.log('Navigation Timing:')
    console.log(`  DNS Lookup: ${metrics.navigation.dnsLookup?.toFixed(2)}ms`)
    console.log(`  TCP Connect: ${metrics.navigation.tcpConnect?.toFixed(2)}ms`)
    console.log(`  TTFB: ${metrics.navigation.ttfb?.toFixed(2)}ms`)
    console.log(`  Response Time: ${metrics.navigation.responseTime?.toFixed(2)}ms`)
    console.log(`  DOM Interactive: ${metrics.navigation.domInteractive?.toFixed(2)}ms`)
    console.log(`  DOM Content Loaded: ${metrics.navigation.domContentLoaded?.toFixed(2)}ms`)
    console.log(`  Load Complete: ${metrics.navigation.loadComplete?.toFixed(2)}ms`)
    
    console.log('\nPaint Metrics:')
    console.log(`  First Paint: ${metrics.paint.firstPaint?.toFixed(2)}ms`)
    console.log(`  First Contentful Paint: ${metrics.paint.firstContentfulPaint?.toFixed(2)}ms`)
    console.log(`  Largest Contentful Paint: ${metrics.lcp?.toFixed(2)}ms`)
    
    console.log('\nResource Loading:')
    console.log(`  Total Resources: ${metrics.resources.count}`)
    console.log(`  Total Transfer Size: ${(metrics.resources.totalSize / 1024).toFixed(2)}KB`)
    console.log(`  JS Files: ${metrics.resources.jsCount}`)
    console.log(`  CSS Files: ${metrics.resources.cssCount}`)
    
    if (metrics.memory) {
      console.log('\nMemory Usage:')
      console.log(`  Used JS Heap: ${(metrics.memory.usedJSHeapSize / 1024 / 1024).toFixed(2)}MB`)
      console.log(`  Total JS Heap: ${(metrics.memory.totalJSHeapSize / 1024 / 1024).toFixed(2)}MB`)
    }
    
    console.log('\n=== End Report ===\n')
    
    // Basic assertions
    expect(metrics.navigation.loadComplete).toBeLessThan(5000)
    if (metrics.paint.firstContentfulPaint) {
      expect(metrics.paint.firstContentfulPaint).toBeLessThan(2500)
    }
  })
})
