# Copilot Instructions for ANXAgentSwarm

## Project Overview

ANXAgentSwarm is an Ollama-based multi-agent system built with .NET 8 (C#) and Vue 3 (TypeScript). It orchestrates multiple AI personas to collaboratively solve problems through sequential Q&A interactions.

---

## Technology Stack

### Backend
- **.NET 8** with ASP.NET Core Web API
- **SQLite** with Entity Framework Core
- **SignalR** for real-time communication
- **Ollama** as the LLM provider (default model: gemma3:27b)

### Frontend
- **Vue 3** with Composition API
- **Vite** as build tool
- **TypeScript** (strict mode)
- **Tailwind CSS** for styling
- **SignalR Client** for real-time updates

---

## Architecture Principles

### Backend Architecture
1. **Clean Architecture** with three layers:
   - `ANXAgentSwarm.Core`: Domain entities, interfaces, business logic
   - `ANXAgentSwarm.Infrastructure`: Data access, external services (Ollama, file system)
   - `ANXAgentSwarm.Api`: Controllers, SignalR hubs, API configuration

2. **Dependency Injection**: All services registered in DI container
3. **Repository Pattern**: For all database operations
4. **Interface Segregation**: Small, focused interfaces

### Frontend Architecture
1. **Composition API**: Use `<script setup lang="ts">` syntax
2. **Pinia Stores**: For global state management
3. **Composables**: For reusable logic (useSession, useSignalR)
4. **Type Safety**: Full TypeScript with strict mode

---

## Coding Standards

### C# Standards

```csharp
// Use file-scoped namespaces
namespace ANXAgentSwarm.Core.Entities;

// Use primary constructors for simple classes
public class Memory(Guid id, string identifier, string content)
{
    public Guid Id { get; } = id;
    public string Identifier { get; } = identifier;
    public string Content { get; } = content;
}

// Use records for DTOs
public record CreateSessionRequest(string ProblemStatement);

// Use async/await consistently
public async Task<Session> GetSessionAsync(Guid id)
{
    return await _context.Sessions.FindAsync(id) 
        ?? throw new NotFoundException($"Session {id} not found");
}

// Use expression-bodied members when appropriate
public bool IsCompleted => Status == SessionStatus.Completed;

// Null safety with nullable reference types
public string? FinalSolution { get; set; }
```

### TypeScript/Vue Standards

```typescript
// Use Composition API with script setup
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useSessionStore } from '@/stores/sessionStore'
import type { Message, Session } from '@/types'

// Props with TypeScript
const props = defineProps<{
  sessionId: string
  readonly?: boolean
}>()

// Emits with TypeScript
const emit = defineEmits<{
  (e: 'message-sent', message: Message): void
  (e: 'session-updated', session: Session): void
}>()

// Reactive state
const messages = ref<Message[]>([])
const isLoading = ref(false)

// Computed properties
const sortedMessages = computed(() => 
  [...messages.value].sort((a, b) => 
    new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
  )
)

// Lifecycle hooks
onMounted(async () => {
  await loadMessages()
})
</script>
```

---

## Persona System

### Available Personas
1. **Coordinator** - Entry point, orchestrates the entire problem-solving flow
2. **Business Analyst** - Requirements gathering, business logic analysis
3. **Technical Architect** - System design, architecture decisions
4. **Senior Software Developer** - Complex implementation, code review
5. **Junior Software Developer** - Basic implementation, learning tasks
6. **Senior QA** - Test strategy, complex test cases
7. **Junior QA** - Basic testing, test execution
8. **UX Engineer** - User experience design, workflows
9. **UI Engineer** - Visual design, component styling
10. **Document Writer** - Documentation, user guides

### Persona Response Format
Each persona response must be parsed to extract:
- `Content`: The actual response text
- `InternalReasoning`: Detailed thinking process (shown in UI)
- `ResponseType`: Question, Answer, Delegation, Clarification, Solution
- `DelegateToPersona`: If delegating, which persona
- `DelegationContext`: Specific context for the delegate
- `IsStuck`: If persona cannot proceed

---

## Memory System

### Constraints
- **Max words per memory**: 2000
- **Identifier**: 10 words max
- **Access limit**: Last 10 memories per persona per session

### Memory Operations
```csharp
// Store a new memory
await _memoryService.StoreMemoryAsync(sessionId, PersonaType.Coordinator, 
    "user requirements for todo api", 
    "User wants a REST API with CRUD operations for todo items...");

// Search memories
var memories = await _memoryService.SearchMemoriesAsync(sessionId, 
    PersonaType.TechnicalArchitect, "database schema");

// Get recent memories
var recent = await _memoryService.GetRecentMemoriesAsync(sessionId, 
    PersonaType.SeniorDeveloper, count: 10);
```

---

## Real-Time Communication

### SignalR Events

**Server to Client:**
- `MessageReceived`: New message in session
- `SessionStatusChanged`: Session status updated
- `ClarificationRequested`: Persona needs user input
- `SolutionReady`: Final solution available
- `SessionStuck`: All personas stuck, partial results available

**Client to Server:**
- `JoinSession`: Subscribe to session updates
- `LeaveSession`: Unsubscribe from session

### Frontend SignalR Usage
```typescript
// composables/useSignalR.ts
export function useSignalR() {
  const connection = ref<HubConnection | null>(null)
  
  const connect = async () => {
    connection.value = new HubConnectionBuilder()
      .withUrl('/hubs/session')
      .withAutomaticReconnect()
      .build()
    
    await connection.value.start()
  }
  
  const onMessageReceived = (callback: (message: Message) => void) => {
    connection.value?.on('MessageReceived', callback)
  }
  
  return { connect, onMessageReceived }
}
```

---

## Testing Requirements

### Coverage Target: 100%

### Unit Test Patterns

```csharp
// Use xUnit with FluentAssertions
public class PersonaEngineTests
{
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<IMemoryService> _memoryServiceMock;
    private readonly PersonaEngine _sut;
    
    public PersonaEngineTests()
    {
        _llmProviderMock = new Mock<ILlmProvider>();
        _memoryServiceMock = new Mock<IMemoryService>();
        _sut = new PersonaEngine(_llmProviderMock.Object, _memoryServiceMock.Object);
    }
    
    [Fact]
    public async Task ProcessAsync_WithDelegation_ReturnsDelegationResponse()
    {
        // Arrange
        var message = CreateTestMessage();
        _llmProviderMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>()))
            .ReturnsAsync(new LlmResponse { Content = "[DELEGATE:TechnicalArchitect] Please design the system" });
        
        // Act
        var result = await _sut.ProcessAsync(PersonaType.BusinessAnalyst, message, session, memories);
        
        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
    }
}
```

### Frontend Test Patterns

```typescript
// Use Vitest with Vue Test Utils
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MessageCard from '@/components/MessageCard.vue'

describe('MessageCard', () => {
  it('renders persona name and content', () => {
    const message = {
      id: '1',
      fromPersona: 'Coordinator',
      content: 'Analyzing your request...',
      timestamp: new Date().toISOString()
    }
    
    const wrapper = mount(MessageCard, {
      props: { message }
    })
    
    expect(wrapper.text()).toContain('Coordinator')
    expect(wrapper.text()).toContain('Analyzing your request...')
  })
})
```

---

## File Operations

### Workspace Access
All file operations are restricted to `./workspace` folder.

```csharp
// Correct - use relative paths
await _workspaceService.WriteFileAsync("output/report.md", content);

// The service validates paths are within workspace
public async Task WriteFileAsync(string relativePath, string content)
{
    var fullPath = Path.Combine(_workspaceRoot, relativePath);
    
    // Security: Ensure path is within workspace
    if (!fullPath.StartsWith(_workspaceRoot))
        throw new SecurityException("Access denied: path outside workspace");
    
    await File.WriteAllTextAsync(fullPath, content);
}
```

---

## Common Patterns

### API Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    
    public SessionsController(IAgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }
    
    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionRequest request)
    {
        var session = await _orchestrator.StartSessionAsync(request.ProblemStatement);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session.ToDto());
    }
}
```

### Error Handling
```csharp
// Use custom exceptions
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class PersonaStuckException : Exception
{
    public PersonaStuckException(PersonaType persona, string reason) 
        : base($"{persona} is stuck: {reason}") { }
}

// Global exception handler middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>();
        var response = error?.Error switch
        {
            NotFoundException => new { status = 404, message = error.Error.Message },
            _ => new { status = 500, message = "Internal server error" }
        };
        context.Response.StatusCode = response.status;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## LLM Prompt Engineering

### Persona System Prompt Structure
```
You are {PersonaName}, a {role description}.

## Your Responsibilities
- {responsibility 1}
- {responsibility 2}

## Communication Rules
1. You can delegate to other personas using: [DELEGATE:PersonaType] context
2. You can ask the user for clarification using: [CLARIFY] question
3. You can provide a solution using: [SOLUTION] solution
4. If you cannot proceed, say: [STUCK] reason

## Memory Access
You have access to your memories. Use [REMEMBER:identifier] to recall.
You can store important information with [STORE:identifier] content.

## Current Context
Session: {session_id}
Problem: {problem_statement}
Previous messages: {message_history}
Your memories: {relevant_memories}
```

---

## Do's and Don'ts

### Do's
- ✅ Use dependency injection for all services
- ✅ Write async code throughout
- ✅ Include detailed internal reasoning in persona responses
- ✅ Validate all user inputs
- ✅ Use strongly-typed DTOs for API responses
- ✅ Write tests before or alongside implementation
- ✅ Use meaningful variable and method names
- ✅ Handle all edge cases in orchestrator

### Don'ts
- ❌ Don't use static classes for services
- ❌ Don't expose entities directly in API responses
- ❌ Don't skip error handling
- ❌ Don't hardcode configuration values
- ❌ Don't use `any` type in TypeScript
- ❌ Don't skip memory constraints validation
- ❌ Don't allow file access outside workspace folder
- ❌ Don't process personas in parallel (must be sequential)

---

## Quick Reference

### Create New Persona
1. Add to `PersonaType` enum
2. Create system prompt in configuration
3. Add persona-specific logic in `PersonaEngine`
4. Update tests

### Add New API Endpoint
1. Add method to controller
2. Create request/response DTOs
3. Add service method if needed
4. Write integration tests

### Add SignalR Event
1. Define event in hub interface
2. Implement in hub class
3. Add client handler in Vue composable
4. Update store to handle event
