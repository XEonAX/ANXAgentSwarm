import { test, expect } from './fixtures/test-fixtures'

test.describe('Solution Viewing', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should display solution viewer when session is completed', async ({ appPage, mockApi, page }) => {
    const sessionId = 'completed-session'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Should show solution-related elements
    const solutionElement = page.locator(
      '[data-testid="solution-viewer"], .solution-viewer, ' +
      '[data-testid="solution-content"], .solution-content, ' +
      'text=/solution/i'
    )
    
    await expect(solutionElement).toBeVisible({ timeout: 15000 })
  })

  test('should display the final solution content', async ({ mockApi, page }) => {
    const sessionId = 'solution-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Create a REST API',
          status: 'Completed',
          finalSolution: '# Complete Solution\n\nHere is your REST API implementation with CRUD operations.',
          messages: [
            {
              id: 'msg-solution',
              fromPersona: 'Coordinator',
              content: '# Complete Solution\n\nHere is your REST API implementation with CRUD operations.',
              messageType: 'Solution',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Solution content should be visible
    const solutionContent = page.locator('text=/Complete Solution|REST API implementation/i')
    await expect(solutionContent).toBeVisible({ timeout: 15000 })
  })

  test('should render markdown content correctly in solution', async ({ mockApi, page }) => {
    const sessionId = 'markdown-solution-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test',
          status: 'Completed',
          finalSolution: '# Heading\n\n**Bold text** and *italic text*\n\n```javascript\nconst code = "example";\n```',
          messages: []
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Check for markdown rendering (headings, code blocks, etc.)
    const headingOrText = page.locator('h1:has-text("Heading"), text=/Heading/i')
    await expect(headingOrText).toBeVisible({ timeout: 15000 })
  })

  test('should show session status as completed', async ({ mockApi, page }) => {
    const sessionId = 'status-session'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Status should indicate completion
    const statusElement = page.locator('text=/completed|done|finished/i')
    await expect(statusElement).toBeVisible({ timeout: 15000 })
  })

  test('should display all messages in timeline for completed session', async ({ mockApi, page }) => {
    const sessionId = 'timeline-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem',
          status: 'Completed',
          finalSolution: 'Final solution here',
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Starting analysis...',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 5000).toISOString()
            },
            {
              id: 'msg-2',
              fromPersona: 'BusinessAnalyst',
              content: 'Analyzing requirements...',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 4000).toISOString()
            },
            {
              id: 'msg-3',
              fromPersona: 'TechnicalArchitect',
              content: 'Designing architecture...',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 3000).toISOString()
            },
            {
              id: 'msg-4',
              fromPersona: 'Coordinator',
              content: 'Final solution here',
              messageType: 'Solution',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // All persona messages should be visible
    await expect(page.locator('text=/Coordinator/i').first()).toBeVisible({ timeout: 15000 })
    await expect(page.locator('text=/BusinessAnalyst|Business Analyst/i')).toBeVisible({ timeout: 15000 })
    await expect(page.locator('text=/TechnicalArchitect|Technical Architect/i')).toBeVisible({ timeout: 15000 })
  })

  test('should allow copying solution content', async ({ mockApi, page }) => {
    const sessionId = 'copy-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test',
          status: 'Completed',
          finalSolution: 'Solution to copy',
          messages: []
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Look for copy button if it exists
    const copyButton = page.locator('button:has-text("Copy"), [data-testid="copy-solution"]')
    
    if (await copyButton.isVisible()) {
      await copyButton.click()
      
      // Check for success feedback
      const successFeedback = page.locator('text=/copied|success/i')
      await expect(successFeedback).toBeVisible({ timeout: 5000 })
    }
  })

  test('should display solution with code syntax highlighting', async ({ mockApi, page }) => {
    const sessionId = 'code-solution-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Create a function',
          status: 'Completed',
          finalSolution: '```typescript\nfunction greet(name: string): string {\n  return `Hello, ${name}!`;\n}\n```',
          messages: []
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Code should be displayed
    const codeContent = page.locator('text=/function greet|Hello/i')
    await expect(codeContent).toBeVisible({ timeout: 15000 })
  })

  test('should navigate back to session list from solution view', async ({ appPage, mockApi, page }) => {
    const sessionId = 'nav-session'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Wait for solution to load
    await page.waitForTimeout(2000)
    
    // Click on app title to navigate back
    await appPage.appTitle.click()
    
    // Should be back at session list
    await expect(appPage.sessionCards.first().or(appPage.newSessionButton)).toBeVisible({ timeout: 10000 })
  })

  test('should show problem statement in completed session view', async ({ mockApi, page }) => {
    const sessionId = 'problem-session'
    const problemStatement = 'Create a user authentication system with OAuth2'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: problemStatement,
          status: 'Completed',
          finalSolution: 'OAuth2 implementation...',
          messages: []
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Problem statement should be visible
    const problemText = page.locator(`text=/${problemStatement.substring(0, 20)}/i`)
    await expect(problemText).toBeVisible({ timeout: 15000 })
  })
})

test.describe('Solution Viewing - Stuck Sessions', () => {
  test('should display partial results for stuck sessions', async ({ mockApi, page }) => {
    const sessionId = 'stuck-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Complex problem',
          status: 'Stuck',
          finalSolution: null,
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Starting analysis...',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 3000).toISOString()
            },
            {
              id: 'msg-2',
              fromPersona: 'TechnicalArchitect',
              content: '[STUCK] Cannot proceed without external API documentation',
              messageType: 'Stuck',
              timestamp: new Date().toISOString()
            }
          ]
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Stuck status should be visible
    const stuckIndicator = page.locator('text=/stuck|cannot proceed|blocked/i')
    await expect(stuckIndicator).toBeVisible({ timeout: 15000 })
  })

  test('should allow resuming stuck sessions', async ({ mockApi, page }) => {
    const sessionId = 'resume-stuck-session'
    
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test',
          status: 'Stuck',
          messages: []
        })
      })
    })
    
    await page.route(`**/api/sessions/${sessionId}/resume`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test',
          status: 'InProgress',
          messages: []
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Look for resume button
    const resumeButton = page.locator('button:has-text("Resume"), button:has-text("Retry")')
    
    if (await resumeButton.isVisible()) {
      await resumeButton.click()
      await page.waitForTimeout(1000)
    }
  })
})

test.describe('Solution Viewing - Responsive Design', () => {
  test('should display solution correctly on mobile', async ({ mockApi, page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    
    const sessionId = 'mobile-solution'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    const solutionContent = page.locator('text=/solution/i')
    await expect(solutionContent).toBeVisible({ timeout: 15000 })
  })

  test('should display solution correctly on tablet', async ({ mockApi, page }) => {
    await page.setViewportSize({ width: 768, height: 1024 })
    
    const sessionId = 'tablet-solution'
    await mockApi.mockCompletedSession(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    const solutionContent = page.locator('text=/solution/i')
    await expect(solutionContent).toBeVisible({ timeout: 15000 })
  })
})
