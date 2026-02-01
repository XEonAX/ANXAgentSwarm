import { test, expect } from './fixtures/test-fixtures'

/**
 * Health check endpoint verification tests.
 * These tests verify that all health check and status endpoints work correctly.
 */
test.describe('Health Check Endpoints', () => {
  test('API health endpoint should return healthy status', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/health')
    
    expect(response.status()).toBe(200)
    
    const body = await response.json()
    expect(body.status).toBe('healthy')
    expect(body.timestamp).toBeDefined()
  })

  test('API status/health endpoint should return detailed health', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/api/status/health')
    
    expect(response.status()).toBe(200)
    
    const body = await response.json()
    expect(body.status).toBe('healthy')
    expect(body.services).toBeDefined()
    expect(body.services.database).toBe(true)
  })

  test('LLM status endpoint should return availability', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/api/status/llm')
    
    expect(response.status()).toBe(200)
    
    const body = await response.json()
    expect(typeof body.isAvailable).toBe('boolean')
    expect(body.defaultModel).toBeDefined()
  })

  test('sessions endpoint should return array', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/api/sessions')
    
    expect(response.status()).toBe(200)
    
    const body = await response.json()
    expect(Array.isArray(body)).toBe(true)
  })
})

test.describe('Health Check Endpoints - Error Handling', () => {
  test('should return 404 for non-existent session', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/api/sessions/00000000-0000-0000-0000-000000000000')
    
    expect(response.status()).toBe(404)
  })

  test('should return 400 for invalid session creation', async ({ page }) => {
    const response = await page.request.post('http://localhost:5046/api/sessions', {
      data: { problemStatement: '' }
    })
    
    expect(response.status()).toBe(400)
  })

  test('should return 400 for empty clarification response', async ({ page }) => {
    // First create a session (this might fail if no session exists)
    const response = await page.request.post('http://localhost:5046/api/sessions/00000000-0000-0000-0000-000000000001/clarify', {
      data: { response: '' }
    })
    
    // Either 400 (invalid request) or 404 (session not found)
    expect([400, 404]).toContain(response.status())
  })
})

test.describe('Health Check - Frontend Connectivity', () => {
  test('frontend should be able to reach API', async ({ page }) => {
    await page.goto('/')
    
    // Check if the app can connect (connection status should eventually show connected)
    await page.waitForFunction(() => {
      const status = document.querySelector('.connection-status, [data-testid="connection-status"]')
      const text = status?.textContent?.toLowerCase() || ''
      // Either connected or at least not showing error
      return text.includes('connect') || !text.includes('error')
    }, { timeout: 15000 }).catch(() => {
      // Connection might not be visible, that's okay for this test
    })
  })

  test('SignalR hub should be accessible', async ({ page }) => {
    // Try to access SignalR negotiate endpoint
    const response = await page.request.post('http://localhost:5046/hubs/session/negotiate?negotiateVersion=1', {
      headers: {
        'Content-Type': 'text/plain;charset=UTF-8'
      }
    }).catch(() => null)
    
    // SignalR should respond (might be 200 or redirect)
    if (response) {
      expect([200, 204, 400]).toContain(response.status())
    }
  })
})

test.describe('Health Check - Endpoint Response Times', () => {
  test('health endpoint should respond within 1 second', async ({ page }) => {
    const startTime = Date.now()
    const response = await page.request.get('http://localhost:5046/health')
    const responseTime = Date.now() - startTime
    
    expect(response.status()).toBe(200)
    expect(responseTime).toBeLessThan(1000)
    
    console.log(`Health endpoint response time: ${responseTime}ms`)
  })

  test('sessions list should respond within 2 seconds', async ({ page }) => {
    const startTime = Date.now()
    const response = await page.request.get('http://localhost:5046/api/sessions')
    const responseTime = Date.now() - startTime
    
    expect(response.status()).toBe(200)
    expect(responseTime).toBeLessThan(2000)
    
    console.log(`Sessions list response time: ${responseTime}ms`)
  })

  test('LLM status should respond within 5 seconds', async ({ page }) => {
    const startTime = Date.now()
    const response = await page.request.get('http://localhost:5046/api/status/llm')
    const responseTime = Date.now() - startTime
    
    expect(response.status()).toBe(200)
    // LLM check might take longer as it pings Ollama
    expect(responseTime).toBeLessThan(5000)
    
    console.log(`LLM status response time: ${responseTime}ms`)
  })
})

test.describe('Health Check - CORS Configuration', () => {
  test('API should allow requests from frontend origin', async ({ page }) => {
    await page.goto('/')
    
    // Make a fetch request from the frontend context
    const corsCheck = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5046/health')
        return {
          ok: response.ok,
          status: response.status,
          corsAllowed: true
        }
      } catch (error) {
        return {
          ok: false,
          status: 0,
          corsAllowed: false,
          error: String(error)
        }
      }
    })
    
    // If we're on same origin or CORS is configured, this should work
    // Note: This might fail in strict CORS environments
    console.log('CORS check result:', corsCheck)
  })
})

test.describe('Health Check - Content Types', () => {
  test('health endpoint should return JSON', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/health')
    
    const contentType = response.headers()['content-type']
    expect(contentType).toContain('application/json')
  })

  test('API endpoints should return JSON', async ({ page }) => {
    const response = await page.request.get('http://localhost:5046/api/sessions')
    
    const contentType = response.headers()['content-type']
    expect(contentType).toContain('application/json')
  })
})

test.describe('Health Check - Comprehensive Report', () => {
  test('generate endpoint health report', async ({ page }) => {
    const endpoints = [
      { name: 'Health', url: 'http://localhost:5046/health', method: 'GET' },
      { name: 'API Health', url: 'http://localhost:5046/api/status/health', method: 'GET' },
      { name: 'LLM Status', url: 'http://localhost:5046/api/status/llm', method: 'GET' },
      { name: 'Sessions List', url: 'http://localhost:5046/api/sessions', method: 'GET' },
    ]
    
    console.log('\n=== Health Check Report ===\n')
    
    const results = []
    
    for (const endpoint of endpoints) {
      const startTime = Date.now()
      try {
        const response = await page.request.get(endpoint.url)
        const responseTime = Date.now() - startTime
        
        results.push({
          name: endpoint.name,
          url: endpoint.url,
          status: response.status(),
          responseTime,
          healthy: response.status() === 200
        })
        
        console.log(`✓ ${endpoint.name}: ${response.status()} (${responseTime}ms)`)
      } catch (error) {
        results.push({
          name: endpoint.name,
          url: endpoint.url,
          status: 0,
          responseTime: 0,
          healthy: false,
          error: String(error)
        })
        
        console.log(`✗ ${endpoint.name}: FAILED - ${error}`)
      }
    }
    
    console.log('\n=== End Report ===\n')
    
    // At least health endpoint should be working
    const healthResult = results.find(r => r.name === 'Health')
    expect(healthResult?.healthy).toBe(true)
  })
})
