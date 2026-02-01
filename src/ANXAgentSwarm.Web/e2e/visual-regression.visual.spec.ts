import { test, expect } from './fixtures/test-fixtures'

/**
 * Visual regression tests for key UI components.
 * These tests capture screenshots and compare them against baseline snapshots.
 */
test.describe('Visual Regression - Home Page', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('home page visual - desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000) // Wait for animations
    
    await expect(page).toHaveScreenshot('home-desktop.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })

  test('home page visual - tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('home-tablet.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })

  test('home page visual - mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('home-mobile.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Session List', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('session list with items', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    
    // Wait for session cards to load
    await page.waitForSelector('[data-testid="session-card"], .session-card', { timeout: 10000 })
    await page.waitForTimeout(500)
    
    const sessionList = page.locator('[data-testid="session-list"], .session-list, main')
    await expect(sessionList).toHaveScreenshot('session-list.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })

  test('empty session list', async ({ page }) => {
    await page.route('**/api/sessions', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        })
      }
    })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('session-list-empty.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Session Creation', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('session creation form', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.click()
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)
    
    await expect(page).toHaveScreenshot('session-creation-form.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })

  test('session creation form with text', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    
    const newSessionButton = page.getByRole('button', { name: /new session|create/i })
    await newSessionButton.click()
    
    const textarea = page.locator('textarea')
    await textarea.fill('Create a REST API for a todo application with CRUD operations')
    await page.waitForTimeout(300)
    
    await expect(page).toHaveScreenshot('session-creation-with-text.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Session View', () => {
  test('session view with messages', async ({ mockApi, page }) => {
    const sessionId = 'visual-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Create a REST API',
          status: 'InProgress',
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Analyzing your request for a REST API...',
              internalReasoning: 'I need to understand the requirements first.',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 5000).toISOString()
            },
            {
              id: 'msg-2',
              fromPersona: 'BusinessAnalyst',
              content: 'I will gather the requirements for your REST API.',
              internalReasoning: 'Starting requirements analysis.',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 3000).toISOString()
            },
            {
              id: 'msg-3',
              fromPersona: 'TechnicalArchitect',
              content: 'Designing the API architecture with RESTful endpoints.',
              internalReasoning: 'Planning the system design.',
              messageType: 'Processing',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('session-view-messages.png', {
      maxDiffPixels: 150,
      threshold: 0.2
    })
  })

  test('message card with internal reasoning', async ({ mockApi, page }) => {
    const sessionId = 'reasoning-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test',
          status: 'InProgress',
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Processing...',
              internalReasoning: 'This is the detailed internal reasoning that helps understand the thought process of the persona.',
              messageType: 'Processing',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    
    // Click to expand reasoning if there's a toggle
    const reasoningToggle = page.locator('button:has-text("reasoning"), [data-testid="toggle-reasoning"]')
    if (await reasoningToggle.isVisible()) {
      await reasoningToggle.click()
      await page.waitForTimeout(300)
    }
    
    const messageCard = page.locator('[data-testid="message-card"], .message-card').first()
    await expect(messageCard).toHaveScreenshot('message-card-reasoning.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Clarification Dialog', () => {
  test('clarification dialog visual', async ({ mockApi, page }) => {
    const sessionId = 'clarify-visual-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('clarification-dialog.png', {
      maxDiffPixels: 100,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Solution Viewer', () => {
  test('solution viewer visual', async ({ mockApi, page }) => {
    const sessionId = 'solution-visual-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Create a REST API',
          status: 'Completed',
          finalSolution: `# REST API Solution

## Overview
Here is your complete REST API implementation for a todo application.

## Endpoints

### GET /api/todos
Returns all todos.

### POST /api/todos
Creates a new todo.

\`\`\`typescript
interface Todo {
  id: string;
  title: string;
  completed: boolean;
}
\`\`\`

### PUT /api/todos/:id
Updates an existing todo.

### DELETE /api/todos/:id
Deletes a todo.
`,
          messages: []
        })
      })
    })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('solution-viewer.png', {
      maxDiffPixels: 150,
      threshold: 0.2
    })
  })

  test('solution with code blocks', async ({ mockApi, page }) => {
    const sessionId = 'code-solution-visual'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Create a function',
          status: 'Completed',
          finalSolution: `# Code Solution

\`\`\`typescript
export function greet(name: string): string {
  return \`Hello, \${name}!\`;
}

export function add(a: number, b: number): number {
  return a + b;
}
\`\`\`
`,
          messages: []
        })
      })
    })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('solution-code-blocks.png', {
      maxDiffPixels: 150,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Persona Avatars', () => {
  test('all persona avatars', async ({ mockApi, page }) => {
    const sessionId = 'personas-visual'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test all personas',
          status: 'InProgress',
          messages: [
            { id: 'm1', fromPersona: 'Coordinator', content: 'Message 1', messageType: 'Processing', timestamp: new Date(Date.now() - 10000).toISOString() },
            { id: 'm2', fromPersona: 'BusinessAnalyst', content: 'Message 2', messageType: 'Processing', timestamp: new Date(Date.now() - 9000).toISOString() },
            { id: 'm3', fromPersona: 'TechnicalArchitect', content: 'Message 3', messageType: 'Processing', timestamp: new Date(Date.now() - 8000).toISOString() },
            { id: 'm4', fromPersona: 'SeniorDeveloper', content: 'Message 4', messageType: 'Processing', timestamp: new Date(Date.now() - 7000).toISOString() },
            { id: 'm5', fromPersona: 'JuniorDeveloper', content: 'Message 5', messageType: 'Processing', timestamp: new Date(Date.now() - 6000).toISOString() },
            { id: 'm6', fromPersona: 'SeniorQA', content: 'Message 6', messageType: 'Processing', timestamp: new Date(Date.now() - 5000).toISOString() },
            { id: 'm7', fromPersona: 'JuniorQA', content: 'Message 7', messageType: 'Processing', timestamp: new Date(Date.now() - 4000).toISOString() },
            { id: 'm8', fromPersona: 'UXEngineer', content: 'Message 8', messageType: 'Processing', timestamp: new Date(Date.now() - 3000).toISOString() },
            { id: 'm9', fromPersona: 'UIEngineer', content: 'Message 9', messageType: 'Processing', timestamp: new Date(Date.now() - 2000).toISOString() },
            { id: 'm10', fromPersona: 'DocumentWriter', content: 'Message 10', messageType: 'Processing', timestamp: new Date().toISOString() }
          ]
        })
      })
    })
    
    await page.setViewportSize({ width: 1280, height: 900 })
    await page.goto(`/?session=${sessionId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('all-personas.png', {
      maxDiffPixels: 200,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Connection Status', () => {
  test('connected status', async ({ mockApi, page }) => {
    await mockApi.setupMocks()
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)
    
    const header = page.locator('header')
    await expect(header).toHaveScreenshot('header-connected.png', {
      maxDiffPixels: 50,
      threshold: 0.2
    })
  })
})

test.describe('Visual Regression - Dark Mode', () => {
  test('home page in dark mode (if supported)', async ({ mockApi, page }) => {
    await mockApi.setupMocks()
    
    // Emulate dark color scheme
    await page.emulateMedia({ colorScheme: 'dark' })
    
    await page.setViewportSize({ width: 1280, height: 720 })
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)
    
    await expect(page).toHaveScreenshot('home-dark-mode.png', {
      maxDiffPixels: 150,
      threshold: 0.2
    })
  })
})
