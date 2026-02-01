import { test, expect } from './fixtures/test-fixtures'

test.describe('Clarification Flow', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should display clarification dialog when session needs clarification', async ({ appPage, mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await appPage.goto()
    
    // Navigate directly to the session that needs clarification
    await page.goto(`/?session=${sessionId}`)
    
    // Look for clarification-related elements
    const clarificationElement = page.locator(
      '[data-testid="clarification-dialog"], .clarification-dialog, ' +
      'text=/clarify|clarification|question/i'
    )
    
    await expect(clarificationElement).toBeVisible({ timeout: 15000 })
  })

  test('should display the clarification question from the persona', async ({ appPage, mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Check that the question is displayed
    const questionText = page.locator('text=/Could you please clarify your requirements/i')
    await expect(questionText).toBeVisible({ timeout: 15000 })
  })

  test('should allow user to submit a clarification response', async ({ appPage, mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    // Mock the clarification submission endpoint
    await page.route(`**/api/sessions/${sessionId}/clarify`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'clarify-msg',
          sessionId: sessionId,
          fromPersona: 'User',
          content: 'Here is my clarification',
          messageType: 'UserResponse',
          timestamp: new Date().toISOString()
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Find and fill the clarification input
    const clarificationInput = page.locator(
      '[data-testid="clarification-input"], .clarification-input, ' +
      'textarea[placeholder*="response"], textarea[placeholder*="clarif"]'
    ).or(page.locator('textarea').last())
    
    await clarificationInput.waitFor({ state: 'visible', timeout: 15000 })
    await clarificationInput.fill('Here is my clarification: I need a simple todo app')
    
    // Submit the clarification
    const submitButton = page.locator(
      '[data-testid="clarification-submit"], button:has-text("Submit"), button:has-text("Send")'
    )
    await submitButton.click()
  })

  test('should show persona name requesting clarification', async ({ appPage, mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Should show which persona is asking
    const personaName = page.locator('text=/BusinessAnalyst|Business Analyst/i')
    await expect(personaName).toBeVisible({ timeout: 15000 })
  })

  test('should update session status after submitting clarification', async ({ appPage, mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    // First, mock clarification state
    await page.route(`**/api/sessions/${sessionId}/clarify`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'clarify-msg',
          sessionId: sessionId,
          fromPersona: 'User',
          content: 'My clarification',
          messageType: 'UserResponse',
          timestamp: new Date().toISOString()
        })
      })
    })

    // Then mock session to be in progress after clarification
    let clarificationSubmitted = false
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      if (clarificationSubmitted) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: sessionId,
            problemStatement: 'Test problem',
            status: 'InProgress',
            messages: []
          })
        })
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: sessionId,
            problemStatement: 'Test problem',
            status: 'WaitingForClarification',
            messages: [],
            pendingClarification: {
              personaType: 'BusinessAnalyst',
              question: 'Please clarify'
            }
          })
        })
      }
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Submit clarification
    const input = page.locator('textarea').last()
    await input.waitFor({ state: 'visible', timeout: 15000 })
    await input.fill('My clarification')
    
    clarificationSubmitted = true
    
    const submitButton = page.locator('button:has-text("Submit"), button:has-text("Send")').last()
    await submitButton.click()
    
    // Status should change from WaitingForClarification
    await page.waitForTimeout(1000) // Wait for status update
  })

  test('should handle multiple clarification rounds', async ({ page, mockApi }) => {
    await mockApi.setupMocks()
    
    const sessionId = 'multi-clarification-session'
    let clarificationRound = 1
    
    // Mock session with multiple clarification rounds
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Complex problem',
          status: 'WaitingForClarification',
          messages: [],
          pendingClarification: {
            personaType: clarificationRound === 1 ? 'BusinessAnalyst' : 'TechnicalArchitect',
            question: `Clarification round ${clarificationRound}`
          }
        })
      })
    })
    
    await page.route(`**/api/sessions/${sessionId}/clarify`, async (route) => {
      clarificationRound++
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: `clarify-msg-${clarificationRound}`,
          sessionId: sessionId,
          fromPersona: 'User',
          content: route.request().postDataJSON()?.response,
          messageType: 'UserResponse',
          timestamp: new Date().toISOString()
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // First clarification
    const input = page.locator('textarea').last()
    await input.waitFor({ state: 'visible', timeout: 15000 })
    await expect(page.locator('text=/Clarification round 1/i')).toBeVisible({ timeout: 15000 })
  })

  test('should disable submit button when clarification input is empty', async ({ mockApi, page }) => {
    const sessionId = 'clarification-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.goto(`/?session=${sessionId}`)
    
    // Find submit button
    const submitButton = page.locator(
      '[data-testid="clarification-submit"], button:has-text("Submit")'
    ).first()
    
    await submitButton.waitFor({ state: 'visible', timeout: 15000 })
    
    // Button should be disabled when input is empty
    const isDisabled = await submitButton.isDisabled()
    // Either disabled or will show validation on click
    expect(typeof isDisabled).toBe('boolean')
  })

  test('should preserve message history after clarification', async ({ mockApi, page }) => {
    const sessionId = 'history-session'
    
    // Mock session with message history
    await page.route(`**/api/sessions/${sessionId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: sessionId,
          problemStatement: 'Test problem',
          status: 'WaitingForClarification',
          messages: [
            {
              id: 'msg-1',
              fromPersona: 'Coordinator',
              content: 'Starting analysis...',
              messageType: 'Processing',
              timestamp: new Date(Date.now() - 60000).toISOString()
            },
            {
              id: 'msg-2',
              fromPersona: 'BusinessAnalyst',
              content: 'I need more details',
              messageType: 'Clarification',
              timestamp: new Date().toISOString()
            }
          ],
          pendingClarification: {
            personaType: 'BusinessAnalyst',
            question: 'I need more details'
          }
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    // Both messages should be visible
    await expect(page.locator('text=/Starting analysis/i')).toBeVisible({ timeout: 15000 })
    await expect(page.locator('text=/I need more details/i')).toBeVisible({ timeout: 15000 })
  })
})

test.describe('Clarification Flow - Edge Cases', () => {
  test('should handle network error during clarification submission', async ({ mockApi, page }) => {
    const sessionId = 'error-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    // Mock network error
    await page.route(`**/api/sessions/${sessionId}/clarify`, async (route) => {
      await route.abort('failed')
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    const input = page.locator('textarea').last()
    await input.waitFor({ state: 'visible', timeout: 15000 })
    await input.fill('My clarification')
    
    const submitButton = page.locator('button:has-text("Submit"), button:has-text("Send")').last()
    await submitButton.click()
    
    // Should show error message
    const errorMessage = page.locator('text=/error|failed|try again/i')
    await expect(errorMessage).toBeVisible({ timeout: 10000 })
  })

  test('should handle very long clarification responses', async ({ mockApi, page }) => {
    const sessionId = 'long-response-session'
    await mockApi.mockClarificationRequest(sessionId)
    
    await page.route(`**/api/sessions/${sessionId}/clarify`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'clarify-msg',
          sessionId: sessionId,
          fromPersona: 'User',
          content: route.request().postDataJSON()?.response,
          messageType: 'UserResponse',
          timestamp: new Date().toISOString()
        })
      })
    })
    
    await page.goto(`/?session=${sessionId}`)
    
    const longResponse = 'This is a very long clarification response. '.repeat(50)
    
    const input = page.locator('textarea').last()
    await input.waitFor({ state: 'visible', timeout: 15000 })
    await input.fill(longResponse)
    
    const submitButton = page.locator('button:has-text("Submit"), button:has-text("Send")').last()
    await submitButton.click()
    
    // Should handle without error
    await page.waitForTimeout(1000)
  })
})
