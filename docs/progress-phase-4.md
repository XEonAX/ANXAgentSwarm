# ANXAgentSwarm - Phase 4: SignalR Integration ✅ COMPLETED

**Completed:** 2026-02-01

## What Was Built

### 1. ISessionHubBroadcaster Interface
**File:** [src/ANXAgentSwarm.Core/Interfaces/ISessionHubBroadcaster.cs](../src/ANXAgentSwarm.Core/Interfaces/ISessionHubBroadcaster.cs)

An abstraction layer that allows the Infrastructure layer to broadcast SignalR events without a direct dependency on the API layer.

#### Methods
- `BroadcastMessageReceivedAsync()` - Notify clients when a new message is created
- `BroadcastSessionStatusChangedAsync()` - Notify clients when session status changes
- `BroadcastClarificationRequestedAsync()` - Notify clients when user input is needed
- `BroadcastSolutionReadyAsync()` - Notify clients when final solution is ready
- `BroadcastSessionStuckAsync()` - Notify clients when session cannot proceed

### 2. SessionHub
**File:** [src/ANXAgentSwarm.Api/Hubs/SessionHub.cs](../src/ANXAgentSwarm.Api/Hubs/SessionHub.cs)

The SignalR hub that manages real-time communication with clients.

#### Client-to-Server Methods
- `JoinSession(sessionId)` - Subscribe to updates for a specific session
- `LeaveSession(sessionId)` - Unsubscribe from session updates

#### Server-to-Client Events
- `MessageReceived` - New message in the session
- `SessionStatusChanged` - Session status has changed
- `ClarificationRequested` - A persona needs user input
- `SolutionReady` - Final solution is available
- `SessionStuck` - Session cannot proceed, partial results available
- `JoinedSession` - Confirmation of joining a session room
- `LeftSession` - Confirmation of leaving a session room

#### Features
- Group-based messaging (clients join session-specific rooms)
- Connection/disconnection logging
- Error handling for disconnections

### 3. SessionHubBroadcaster
**File:** [src/ANXAgentSwarm.Api/Hubs/SessionHubBroadcaster.cs](../src/ANXAgentSwarm.Api/Hubs/SessionHubBroadcaster.cs)

Implementation of `ISessionHubBroadcaster` that bridges the Infrastructure layer with SignalR.

#### Key Design Decisions
- Uses `IHubContext<SessionHub>` for out-of-hub broadcasting
- Targets session groups for efficient message delivery
- Includes structured logging for debugging

### 4. AgentOrchestrator Updates
**File:** [src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs](../src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs)

Integrated SignalR broadcasting throughout the orchestration flow.

#### Broadcast Points

| Event | When Triggered |
|-------|---------------|
| `MessageReceived` | Any message created (user input, persona response) |
| `SessionStatusChanged` | Session transitions between states |
| `ClarificationRequested` | Persona asks for user clarification |
| `SolutionReady` | Session completes with final solution |
| `SessionStuck` | All personas stuck, partial results compiled |

### 5. Program.cs Configuration
**File:** [src/ANXAgentSwarm.Api/Program.cs](../src/ANXAgentSwarm.Api/Program.cs)

#### SignalR Configuration
```csharp
builder.Services.AddSignalR();
builder.Services.AddScoped<ISessionHubBroadcaster, SessionHubBroadcaster>();

app.MapHub<SessionHub>("/hubs/session");
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Vue 3 Frontend                                  │
│                     (SignalR Client - @microsoft/signalr)                   │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ WebSocket Connection
                                 │ wss://localhost:5001/hubs/session
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SessionHub                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Client Methods:                                                          ││
│  │   JoinSession(sessionId) → Groups.AddToGroupAsync()                     ││
│  │   LeaveSession(sessionId) → Groups.RemoveFromGroupAsync()               ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                 ▲
                                 │ IHubContext<SessionHub>
                                 │
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SessionHubBroadcaster                                │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Implements: ISessionHubBroadcaster                                       ││
│  │                                                                          ││
│  │ BroadcastMessageReceivedAsync(sessionId, message)                       ││
│  │   → _hubContext.Clients.Group(sessionId).SendAsync("MessageReceived")   ││
│  │                                                                          ││
│  │ BroadcastSessionStatusChangedAsync(sessionId, session)                  ││
│  │   → _hubContext.Clients.Group(sessionId).SendAsync("SessionStatus...")  ││
│  │                                                                          ││
│  │ BroadcastClarificationRequestedAsync(sessionId, message)                ││
│  │   → _hubContext.Clients.Group(sessionId).SendAsync("Clarification...")  ││
│  │                                                                          ││
│  │ BroadcastSolutionReadyAsync(sessionId, session)                         ││
│  │   → _hubContext.Clients.Group(sessionId).SendAsync("SolutionReady")     ││
│  │                                                                          ││
│  │ BroadcastSessionStuckAsync(sessionId, session, partialResults)          ││
│  │   → _hubContext.Clients.Group(sessionId).SendAsync("SessionStuck")      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                 ▲
                                 │ ISessionHubBroadcaster (DI)
                                 │
┌─────────────────────────────────────────────────────────────────────────────┐
│                           AgentOrchestrator                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Injects: ISessionHubBroadcaster _hubBroadcaster                         ││
│  │                                                                          ││
│  │ Broadcasts during:                                                       ││
│  │   - StartSessionAsync(): Initial message                                ││
│  │   - HandleUserClarificationAsync(): User response + status change       ││
│  │   - ProcessWithPersonaAsync(): Every persona response                   ││
│  │   - HandleSolutionAsync(): Solution ready                               ││
│  │   - HandleClarificationAsync(): Clarification needed                    ││
│  │   - HandleAllStuckAsync(): Session stuck                                ││
│  │   - HandleDeclineAsync(): Coordinator alternative response              ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Event Flow Examples

### 1. Normal Session Flow
```
Client                  Hub                    Orchestrator
   │                     │                          │
   │── JoinSession(id) ──▶                          │
   │◀── JoinedSession ───│                          │
   │                     │                          │
   │                     │◀── MessageReceived ──────│ (User problem)
   │◀── MessageReceived ─│                          │
   │                     │                          │
   │                     │◀── MessageReceived ──────│ (Coordinator)
   │◀── MessageReceived ─│                          │
   │                     │                          │
   │                     │◀── MessageReceived ──────│ (BA)
   │◀── MessageReceived ─│                          │
   │                     │                          │
   │                     │ ... more personas ...    │
   │                     │                          │
   │                     │◀── SolutionReady ────────│
   │◀── SolutionReady ───│                          │
```

### 2. Clarification Flow
```
Client                  Hub                    Orchestrator
   │                     │                          │
   │                     │◀── ClarificationReq ─────│ (Persona needs input)
   │                     │◀── SessionStatusChanged ─│ (WaitingForClarification)
   │◀── ClarificationReq │                          │
   │◀── StatusChanged ───│                          │
   │                     │                          │
   │── POST /clarify ───────────────────────────────▶
   │                     │                          │
   │                     │◀── MessageReceived ──────│ (User response)
   │                     │◀── SessionStatusChanged ─│ (Active)
   │◀── MessageReceived ─│                          │
   │◀── StatusChanged ───│                          │
```

### 3. Stuck Session Flow
```
Client                  Hub                    Orchestrator
   │                     │                          │
   │                     │◀── MessageReceived ──────│ (Stuck persona)
   │◀── MessageReceived ─│                          │
   │                     │                          │
   │                     │ ... more stuck ...       │
   │                     │                          │
   │                     │◀── SessionStuck ─────────│ (All stuck)
   │◀── SessionStuck ────│                          │
   │                     │                          │
   │ (receives partial results and guidance)        │
```

---

## Frontend Integration Guide

### JavaScript/TypeScript Client Setup
```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5001/hubs/session')
    .withAutomaticReconnect()
    .build();

// Event handlers
connection.on('MessageReceived', (message: MessageDto) => {
    console.log('New message:', message);
    // Add to message list
});

connection.on('SessionStatusChanged', (session: SessionDto) => {
    console.log('Status changed:', session.status);
    // Update session state
});

connection.on('ClarificationRequested', (message: MessageDto) => {
    console.log('Clarification needed:', message);
    // Show clarification prompt
});

connection.on('SolutionReady', (session: SessionDto) => {
    console.log('Solution ready:', session.finalSolution);
    // Display final solution
});

connection.on('SessionStuck', (data: { session: SessionDto, partialResults: string }) => {
    console.log('Session stuck:', data.partialResults);
    // Show partial results and options
});

// Connect and join session
await connection.start();
await connection.invoke('JoinSession', sessionId);
```

### Vue 3 Composable
```typescript
// composables/useSignalR.ts
import { ref, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

export function useSignalR() {
    const connection = ref<signalR.HubConnection | null>(null);
    const isConnected = ref(false);

    const connect = async () => {
        connection.value = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/session')
            .withAutomaticReconnect()
            .build();

        await connection.value.start();
        isConnected.value = true;
    };

    const joinSession = async (sessionId: string) => {
        await connection.value?.invoke('JoinSession', sessionId);
    };

    const leaveSession = async (sessionId: string) => {
        await connection.value?.invoke('LeaveSession', sessionId);
    };

    const onMessageReceived = (callback: (message: MessageDto) => void) => {
        connection.value?.on('MessageReceived', callback);
    };

    const onClarificationRequested = (callback: (message: MessageDto) => void) => {
        connection.value?.on('ClarificationRequested', callback);
    };

    const onSolutionReady = (callback: (session: SessionDto) => void) => {
        connection.value?.on('SolutionReady', callback);
    };

    const onSessionStuck = (callback: (data: any) => void) => {
        connection.value?.on('SessionStuck', callback);
    };

    onUnmounted(() => {
        connection.value?.stop();
    });

    return {
        connect,
        joinSession,
        leaveSession,
        onMessageReceived,
        onClarificationRequested,
        onSolutionReady,
        onSessionStuck,
        isConnected
    };
}
```

---

## Files Created/Modified

### New Files
1. [src/ANXAgentSwarm.Core/Interfaces/ISessionHubBroadcaster.cs](../src/ANXAgentSwarm.Core/Interfaces/ISessionHubBroadcaster.cs)
2. [src/ANXAgentSwarm.Api/Hubs/SessionHub.cs](../src/ANXAgentSwarm.Api/Hubs/SessionHub.cs)
3. [src/ANXAgentSwarm.Api/Hubs/SessionHubBroadcaster.cs](../src/ANXAgentSwarm.Api/Hubs/SessionHubBroadcaster.cs)

### Modified Files
1. [src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs](../src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs) - Added hub broadcaster injection and event broadcasting
2. [src/ANXAgentSwarm.Api/Program.cs](../src/ANXAgentSwarm.Api/Program.cs) - Added SignalR configuration and hub mapping

---

## Testing Considerations

### Unit Tests for AgentOrchestrator
```csharp
[Fact]
public async Task StartSessionAsync_BroadcastsMessageReceived()
{
    // Arrange
    var hubBroadcasterMock = new Mock<ISessionHubBroadcaster>();
    var orchestrator = new AgentOrchestrator(
        _sessionRepoMock.Object,
        _messageRepoMock.Object,
        _memoryServiceMock.Object,
        _personaEngineMock.Object,
        hubBroadcasterMock.Object,
        _loggerMock.Object);

    // Act
    await orchestrator.StartSessionAsync("Test problem");

    // Assert
    hubBroadcasterMock.Verify(
        x => x.BroadcastMessageReceivedAsync(
            It.IsAny<Guid>(),
            It.IsAny<MessageDto>(),
            It.IsAny<CancellationToken>()),
        Times.AtLeastOnce);
}

[Fact]
public async Task HandleClarificationAsync_BroadcastsClarificationRequested()
{
    // Arrange
    // ... setup session with WaitingForClarification status

    // Act
    // ... trigger clarification handling

    // Assert
    _hubBroadcasterMock.Verify(
        x => x.BroadcastClarificationRequestedAsync(
            sessionId,
            It.IsAny<MessageDto>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Integration Tests for SignalR Hub
```csharp
[Fact]
public async Task JoinSession_AddsClientToGroup()
{
    // Arrange
    await using var application = new WebApplicationFactory<Program>();
    var client = application.CreateClient();
    
    var connection = new HubConnectionBuilder()
        .WithUrl($"{client.BaseAddress}hubs/session")
        .Build();

    // Act
    await connection.StartAsync();
    await connection.InvokeAsync("JoinSession", Guid.NewGuid().ToString());

    // Assert
    // Verify client receives JoinedSession confirmation
}
```

---

## Next Steps (Phase 5: Frontend)

The next phase will build the Vue 3 frontend with SignalR client integration:

### Prompt for Phase 5:
```
Continue with Phase 5: Vue 3 Frontend Setup
- Initialize Vue 3 project with Vite in src/ANXAgentSwarm.Web
- Configure TypeScript with strict mode
- Set up Tailwind CSS
- Install and configure @microsoft/signalr client
- Create TypeScript types matching backend DTOs
- Create useSignalR composable for hub connection
- Create useSession composable for session state management
- Create Pinia store for global session state
- Provide the next prompt
- Update docs/progress-phase-5.md after completion
```

---

## Verification

✅ Build passes with `dotnet build`
✅ ISessionHubBroadcaster interface defined in Core layer
✅ SessionHub implemented with JoinSession/LeaveSession methods
✅ SessionHubBroadcaster bridges Infrastructure with SignalR
✅ AgentOrchestrator broadcasts all required events
✅ Program.cs configures SignalR and maps hub endpoint
✅ CORS configured for frontend development
