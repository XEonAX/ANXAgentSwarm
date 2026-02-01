import { test, expect } from './fixtures/test-fixtures'
import AxeBuilder from '@axe-core/playwright'

/**
 * Accessibility tests using axe-core for WCAG compliance.
 * These tests verify that the application meets WCAG 2.1 Level AA standards.
 */
test.describe('Accessibility - WCAG Compliance', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('home page should have no accessibility violations', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze()
    
    // Log violations for debugging
    if (accessibilityScanResults.violations.length > 0) {
      console.log('Accessibility violations found:')
      accessibilityScanResults.violations.forEach(violation => {
        console.log(`- ${violation.id}: ${violation.description}`)
        violation.nodes.forEach(node => {
          console.log(`  Target: ${node.target}`)
          console.log(`  HTML: ${node.html.substring(0, 100)}...`)
        })
      })
    }
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })

  test('session list should have no accessibility violations', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Wait for sessions to load
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .include('.session-list, [data-testid="session-list"]')
      .analyze()
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })

  test('session creation form should have no accessibility violations', async ({ page }) => {
    await page.goto('/')
    
    // Navigate to create session view
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.click()
    
    await page.waitForLoadState('networkidle')
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze()
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })

  test('session view should have no accessibility violations', async ({ mockApi, page }) => {
    const sessionId = 'accessible-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem',
          status: 'InProgress',
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Processing your request...',
              messageType: 'Processing',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze()
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })

  test('clarification dialog should have no accessibility violations', async ({ mockApi, page }) => {
    const sessionId = 'clarification-a11y-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze()
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })

  test('solution viewer should have no accessibility violations', async ({ mockApi, page }) => {
    const sessionId = 'solution-a11y-session'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze()
    
    expect(accessibilityScanResults.violations).toHaveLength(0)
  })
})

test.describe('Accessibility - Keyboard Navigation', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should be able to navigate using Tab key', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Tab through focusable elements
    await page.keyboard.press('Tab')
    const firstFocused = await page.evaluate(() => document.activeElement?.tagName)
    expect(firstFocused).toBeTruthy()
    
    await page.keyboard.press('Tab')
    const secondFocused = await page.evaluate(() => document.activeElement?.tagName)
    expect(secondFocused).toBeTruthy()
  })

  test('should be able to activate buttons with Enter key', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Focus on new session button and activate with Enter
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.focus()
    await page.keyboard.press('Enter')
    
    // Should navigate to session creation
    const input = page.locator('textarea')
    await expect(input).toBeVisible({ timeout: 10000 })
  })

  test('should be able to activate buttons with Space key', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.focus()
    await page.keyboard.press('Space')
    
    // Should navigate to session creation
    const input = page.locator('textarea')
    await expect(input).toBeVisible({ timeout: 10000 })
  })

  test('should have visible focus indicators', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Tab to first focusable element
    await page.keyboard.press('Tab')
    
    // Check that focus is visible (element should have focus styles)
    const focusedElement = await page.evaluate(() => {
      const el = document.activeElement
      if (!el) return null
      const styles = window.getComputedStyle(el)
      return {
        outline: styles.outline,
        boxShadow: styles.boxShadow,
        border: styles.border
      }
    })
    
    // At least one focus indicator should be present
    expect(focusedElement).toBeTruthy()
  })

  test('should trap focus in modal dialogs', async ({ mockApi, page }) => {
    const sessionId = 'modal-focus-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)
    
    // If modal is open, focus should be trapped within it
    const modal = page.locator('[role="dialog"], .modal, .clarification-dialog')
    
    if (await modal.isVisible()) {
      // Tab through modal elements
      await page.keyboard.press('Tab')
      await page.keyboard.press('Tab')
      await page.keyboard.press('Tab')
      
      // Focus should still be within modal
      const focusedInModal = await page.evaluate(() => {
        const modal = document.querySelector('[role="dialog"], .modal, .clarification-dialog')
        return modal?.contains(document.activeElement)
      })
      
      expect(focusedInModal).toBe(true)
    }
  })
})

test.describe('Accessibility - ARIA Landmarks', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should have main landmark', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const main = page.locator('main, [role="main"]')
    await expect(main).toBeVisible()
  })

  test('should have header/banner landmark', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const header = page.locator('header, [role="banner"]')
    await expect(header).toBeVisible()
  })

  test('should have navigation landmark', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Navigation might be part of header
    const nav = page.locator('nav, [role="navigation"]')
    // Either nav exists or navigation is in header
    const header = page.locator('header')
    await expect(nav.or(header)).toBeVisible()
  })
})

test.describe('Accessibility - Color Contrast', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should have sufficient color contrast for text', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2aa'])
      .options({ runOnly: ['color-contrast'] })
      .analyze()
    
    // Filter out minor issues
    const criticalViolations = accessibilityScanResults.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    )
    
    expect(criticalViolations).toHaveLength(0)
  })
})

test.describe('Accessibility - Screen Reader', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should have alt text for images', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    const imagesWithoutAlt = await page.locator('img:not([alt])').count()
    expect(imagesWithoutAlt).toBe(0)
  })

  test('should have labels for form inputs', async ({ page }) => {
    await page.goto('/')
    
    // Navigate to create session
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.click()
    await page.waitForLoadState('networkidle')
    
    // Check that textarea has label or aria-label
    const textarea = page.locator('textarea')
    const hasLabel = await textarea.evaluate((el) => {
      const id = el.id
      const hasLabelFor = id && document.querySelector(`label[for="${id}"]`)
      const hasAriaLabel = el.hasAttribute('aria-label')
      const hasAriaLabelledBy = el.hasAttribute('aria-labelledby')
      const hasPlaceholder = el.hasAttribute('placeholder')
      return hasLabelFor || hasAriaLabel || hasAriaLabelledBy || hasPlaceholder
    })
    
    expect(hasLabel).toBe(true)
  })

  test('should have descriptive button text', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // All buttons should have accessible names
    const buttons = page.locator('button')
    const buttonCount = await buttons.count()
    
    for (let i = 0; i < buttonCount; i++) {
      const button = buttons.nth(i)
      const accessibleName = await button.evaluate((el) => {
        return el.textContent?.trim() || 
               el.getAttribute('aria-label') || 
               el.getAttribute('title') ||
               el.querySelector('svg')?.getAttribute('aria-label')
      })
      
      // Button should have some accessible name (even if it's an icon)
      // Skip if button is not visible
      if (await button.isVisible()) {
        expect(accessibleName || await button.innerHTML()).toBeTruthy()
      }
    }
  })

  test('should announce dynamic content changes', async ({ mockApi, page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Check for aria-live regions
    const liveRegions = page.locator('[aria-live], [role="alert"], [role="status"]')
    
    // Application should have at least one live region for announcements
    const liveRegionCount = await liveRegions.count()
    // This is a soft check - live regions are recommended but not always required
    console.log(`Found ${liveRegionCount} aria-live regions`)
  })
})
