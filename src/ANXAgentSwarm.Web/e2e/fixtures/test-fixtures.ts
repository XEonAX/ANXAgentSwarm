import { test as base, expect, Page, Locator } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

/**
 * Custom page object model for ANXAgentSwarm application.
 */
export class AppPage {
  readonly page: Page
  
  // Main navigation elements
  readonly appTitle: Locator
  readonly connectionStatus: Locator
  readonly newSessionButton: Locator
  
  // Session list elements
  readonly sessionList: Locator
  readonly sessionCards: Locator
  
  // Session view elements
  readonly sessionHeader: Locator
  readonly timeline: Locator
  readonly messageCards: Locator
  readonly userInput: Locator
  readonly submitButton: Locator
  
  // Clarification dialog elements
  readonly clarificationDialog: Locator
  readonly clarificationInput: Locator
  readonly clarificationSubmitButton: Locator
  
  // Solution viewer elements
  readonly solutionViewer: Locator
  readonly solutionContent: Locator

  constructor(page: Page) {
    this.page = page
    
    // Main navigation
    this.appTitle = page.getByRole('heading', { name: /ANXAgentSwarm/i }).or(
      page.locator('h1:has-text("ANXAgentSwarm")')
    )
    this.connectionStatus = page.locator('[data-testid="connection-status"]').or(
      page.locator('.connection-status')
    )
    this.newSessionButton = page.getByRole('button', { name: /new session|create/i })
    
    // Session list
    this.sessionList = page.locator('[data-testid="session-list"]').or(
      page.locator('.session-list')
    )
    this.sessionCards = page.locator('[data-testid="session-card"]').or(
      page.locator('.session-card')
    )
    
    // Session view
    this.sessionHeader = page.locator('[data-testid="session-header"]').or(
      page.locator('.session-header')
    )
    this.timeline = page.locator('[data-testid="timeline"]').or(
      page.locator('.timeline')
    )
    this.messageCards = page.locator('[data-testid="message-card"]').or(
      page.locator('.message-card')
    )
    this.userInput = page.getByRole('textbox', { name: /problem|message/i }).or(
      page.locator('textarea[placeholder*="problem"]')
    )
    this.submitButton = page.getByRole('button', { name: /submit|send|start/i })
    
    // Clarification dialog
    this.clarificationDialog = page.locator('[data-testid="clarification-dialog"]').or(
      page.locator('.clarification-dialog')
    )
    this.clarificationInput = page.locator('[data-testid="clarification-input"]').or(
      page.locator('.clarification-dialog textarea')
    )
    this.clarificationSubmitButton = page.locator('[data-testid="clarification-submit"]').or(
      page.locator('.clarification-dialog button:has-text("Submit")')
    )
    
    // Solution viewer
    this.solutionViewer = page.locator('[data-testid="solution-viewer"]').or(
      page.locator('.solution-viewer')
    )
    this.solutionContent = page.locator('[data-testid="solution-content"]').or(
      page.locator('.solution-content')
    )
  }

  /**
   * Navigate to the application home page.
   */
  async goto() {
    await this.page.goto('/')
    await this.waitForAppReady()
  }

  /**
   * Wait for the application to be fully initialized.
   */
  async waitForAppReady() {
    // Wait for the app title to be visible
    await expect(this.appTitle).toBeVisible({ timeout: 15000 })
    // Wait for connection to be established (green indicator or connected text)
    await this.page.waitForFunction(() => {
      const status = document.querySelector('.connection-status, [data-testid="connection-status"]')
      return status?.textContent?.toLowerCase().includes('connected') ||
             status?.classList.contains('bg-green-100')
    }, { timeout: 15000 }).catch(() => {
      // Connection might not be available in mock mode
    })
  }

  /**
   * Create a new session with the given problem statement.
   */
  async createSession(problemStatement: string) {
    await this.newSessionButton.click()
    await this.userInput.waitFor({ state: 'visible' })
    await this.userInput.fill(problemStatement)
    await this.submitButton.click()
  }

  /**
   * Wait for a message from a specific persona to appear.
   */
  async waitForPersonaMessage(persona: string, timeout = 30000) {
    await this.page.waitForSelector(
      `[data-testid="message-card"]:has-text("${persona}"), .message-card:has-text("${persona}")`,
      { timeout }
    )
  }

  /**
   * Submit a clarification response.
   */
  async submitClarification(response: string) {
    await this.clarificationDialog.waitFor({ state: 'visible' })
    await this.clarificationInput.fill(response)
    await this.clarificationSubmitButton.click()
  }

  /**
   * Get the number of messages in the timeline.
   */
  async getMessageCount(): Promise<number> {
    return await this.messageCards.count()
  }

  /**
   * Check if the solution viewer is displayed.
   */
  async isSolutionVisible(): Promise<boolean> {
    return await this.solutionViewer.isVisible()
  }

  /**
   * Run accessibility scan on the current page.
   */
  async runAccessibilityScan() {
    const accessibilityScanResults = await new AxeBuilder({ page: this.page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze()
    return accessibilityScanResults
  }

  /**
   * Capture performance metrics.
   */
  async getPerformanceMetrics() {
    return await this.page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
      const paint = performance.getEntriesByType('paint')
      const lcp = performance.getEntriesByType('largest-contentful-paint')
      
      return {
        // Navigation timing
        domContentLoaded: navigation?.domContentLoadedEventEnd - navigation?.startTime,
        loadComplete: navigation?.loadEventEnd - navigation?.startTime,
        
        // Paint metrics
        firstPaint: paint.find(p => p.name === 'first-paint')?.startTime,
        firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime,
        
        // LCP
        largestContentfulPaint: lcp[lcp.length - 1]?.startTime,
        
        // Memory (if available)
        // @ts-expect-error memory is not in all browsers
        jsHeapSize: performance.memory?.usedJSHeapSize,
      }
    })
  }

  /**
   * Select a session from the list by index.
   */
  async selectSession(index: number) {
    const sessions = this.sessionCards
    await sessions.nth(index).click()
  }

  /**
   * Get the current session status text.
   */
  async getSessionStatus(): Promise<string> {
    const statusElement = this.page.locator('[data-testid="session-status"], .session-status')
    return await statusElement.textContent() || ''
  }
}

/**
 * Mock API responses for E2E testing without real backend.
 */
export class MockApi {
  constructor(private page: Page) {}

  async setupMocks() {
    // Mock health endpoint
    await this.page.route('**/api/status/health', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          status: 'healthy',
          timestamp: new Date().toISOString(),
          services: { database: true, llm: true }
        })
      })
    })

    // Mock sessions list
    await this.page.route('**/api/sessions', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 'test-session-1',
              problemStatement: 'Test problem 1',
              status: 'Completed',
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString()
            },
            {
              id: 'test-session-2',
              problemStatement: 'Test problem 2',
              status: 'InProgress',
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString()
            }
          ])
        })
      } else if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON()
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'new-session-id',
            problemStatement: body.problemStatement,
            status: 'InProgress',
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            messages: []
          })
        })
      }
    })

    // Mock individual session
    await this.page.route('**/api/sessions/*', async (route) => {
      const url = route.request().url()
      const sessionId = url.split('/').pop()
      
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem statement',
          status: 'InProgress',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          messages: [
            {
              id: 'msg-1',
              sessionId: sessionId,
              fromPersona: 'Coordinator',
              toPersona: null,
              content: 'Analyzing your request...',
              internalReasoning: 'I need to understand the problem first.',
              messageType: 'Processing',
              timestamp: new Date().toISOString()
            }
          ],
          finalSolution: null
        })
      })
    })

    // Mock LLM status
    await this.page.route('**/api/status/llm', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isAvailable: true,
          availableModels: ['gemma3:27b', 'llama2'],
          defaultModel: 'gemma3:27b'
        })
      })
    })
  }

  async mockClarificationRequest(sessionId: string) {
    await this.page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem',
          status: 'WaitingForClarification',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          messages: [
            {
              id: 'msg-clarify',
              sessionId: sessionId,
              fromPersona: 'BusinessAnalyst',
              toPersona: 'User',
              content: 'Could you please clarify your requirements?',
              messageType: 'Clarification',
              timestamp: new Date().toISOString()
            }
          ],
          pendingClarification: {
            personaType: 'BusinessAnalyst',
            question: 'Could you please clarify your requirements?'
          }
        })
      })
    })
  }

  async mockCompletedSession(sessionId: string) {
    await this.page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem',
          status: 'Completed',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          finalSolution: '# Solution\n\nHere is the complete solution...',
          messages: [
            {
              id: 'msg-solution',
              sessionId: sessionId,
              fromPersona: 'Coordinator',
              toPersona: null,
              content: '# Solution\n\nHere is the complete solution...',
              messageType: 'Solution',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
  }
}

/**
 * Extended test fixture with custom page object and mock API.
 */
export const test = base.extend<{
  appPage: AppPage
  mockApi: MockApi
}>({
  appPage: async ({ page }, use) => {
    const appPage = new AppPage(page)
    await use(appPage)
  },
  mockApi: async ({ page }, use) => {
    const mockApi = new MockApi(page)
    await use(mockApi)
  }
})

export { expect }
