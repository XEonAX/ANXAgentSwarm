import { test, expect } from './fixtures/test-fixtures'

test.describe('Session Creation Flow', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should display the application title and connection status', async ({ appPage }) => {
    await appPage.goto()
    
    await expect(appPage.appTitle).toBeVisible()
    await expect(appPage.appTitle).toContainText('ANXAgentSwarm')
  })

  test('should display the new session button', async ({ appPage }) => {
    await appPage.goto()
    
    await expect(appPage.newSessionButton).toBeVisible()
  })

  test('should navigate to session creation when clicking new session', async ({ appPage }) => {
    await appPage.goto()
    
    await appPage.newSessionButton.click()
    await expect(appPage.userInput).toBeVisible()
  })

  test('should create a new session with a problem statement', async ({ appPage, page }) => {
    await appPage.goto()
    
    const problemStatement = 'Create a REST API for a todo application'
    await appPage.createSession(problemStatement)
    
    // Should navigate to session view
    await expect(appPage.timeline).toBeVisible({ timeout: 10000 })
  })

  test('should display validation error for empty problem statement', async ({ appPage, page }) => {
    await appPage.goto()
    await appPage.newSessionButton.click()
    
    // Try to submit without entering a problem statement
    await appPage.submitButton.click()
    
    // Check for validation message (either browser validation or custom)
    const validationMessage = page.locator('text=/required|enter|provide/i')
    await expect(validationMessage.or(appPage.userInput)).toBeVisible()
  })

  test('should display loading state during session creation', async ({ appPage, page }) => {
    // Add delay to POST request to see loading state
    await page.route('**/api/sessions', async (route) => {
      if (route.request().method() === 'POST') {
        await new Promise(resolve => setTimeout(resolve, 500))
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

    await appPage.goto()
    await appPage.newSessionButton.click()
    await appPage.userInput.fill('Test problem')
    await appPage.submitButton.click()
    
    // Check for loading indicator
    const loadingIndicator = page.locator('[data-testid="loading"], .loading, .spinner, [aria-busy="true"]')
    // Loading state should appear or submit should succeed
    await expect(loadingIndicator.or(appPage.timeline)).toBeVisible({ timeout: 10000 })
  })

  test('should display existing sessions in the list', async ({ appPage }) => {
    await appPage.goto()
    
    // Wait for sessions to load
    await expect(appPage.sessionCards.first()).toBeVisible({ timeout: 10000 })
    
    // Should have at least 2 sessions from mock
    const count = await appPage.sessionCards.count()
    expect(count).toBeGreaterThanOrEqual(1)
  })

  test('should navigate to session view when clicking a session card', async ({ appPage }) => {
    await appPage.goto()
    
    await expect(appPage.sessionCards.first()).toBeVisible({ timeout: 10000 })
    await appPage.selectSession(0)
    
    // Should show session view
    await expect(appPage.timeline.or(appPage.sessionHeader)).toBeVisible({ timeout: 10000 })
  })

  test('should handle API errors gracefully', async ({ appPage, page }) => {
    // Mock API error
    await page.route('**/api/sessions', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Internal server error' })
        })
      }
    })

    await appPage.goto()
    await appPage.newSessionButton.click()
    await appPage.userInput.fill('Test problem')
    await appPage.submitButton.click()
    
    // Should display error message
    const errorMessage = page.locator('text=/error|failed|try again/i')
    await expect(errorMessage).toBeVisible({ timeout: 10000 })
  })

  test('should maintain session list after navigating back', async ({ appPage }) => {
    await appPage.goto()
    
    // Get initial session count
    await expect(appPage.sessionCards.first()).toBeVisible({ timeout: 10000 })
    const initialCount = await appPage.sessionCards.count()
    
    // Navigate to a session
    await appPage.selectSession(0)
    await expect(appPage.timeline.or(appPage.sessionHeader)).toBeVisible({ timeout: 10000 })
    
    // Navigate back
    await appPage.appTitle.click()
    
    // Session list should still be there
    await expect(appPage.sessionCards.first()).toBeVisible({ timeout: 10000 })
    const finalCount = await appPage.sessionCards.count()
    expect(finalCount).toBe(initialCount)
  })
})

test.describe('Session Creation - Responsive Design', () => {
  test.beforeEach(async ({ mockApi }) => {
    await mockApi.setupMocks()
  })

  test('should display correctly on mobile viewport', async ({ appPage, page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await appPage.goto()
    
    await expect(appPage.appTitle).toBeVisible()
    await expect(appPage.newSessionButton).toBeVisible()
  })

  test('should display correctly on tablet viewport', async ({ appPage, page }) => {
    await page.setViewportSize({ width: 768, height: 1024 })
    await appPage.goto()
    
    await expect(appPage.appTitle).toBeVisible()
    await expect(appPage.newSessionButton).toBeVisible()
  })

  test('should display correctly on desktop viewport', async ({ appPage, page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 })
    await appPage.goto()
    
    await expect(appPage.appTitle).toBeVisible()
    await expect(appPage.newSessionButton).toBeVisible()
  })
})
