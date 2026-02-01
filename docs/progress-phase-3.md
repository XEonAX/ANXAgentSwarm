# ANXAgentSwarm - Phase 3: Agent Orchestrator ✅ COMPLETED

**Completed:** 2026-02-01

## What Was Built

### 1. AgentOrchestrator Implementation
**File:** [src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs](../src/ANXAgentSwarm.Infrastructure/Services/AgentOrchestrator.cs)

The central service that manages the flow of problem-solving sessions through the persona ecosystem.

#### Core Features

##### StartSessionAsync
- ✅ Creates a new session with auto-generated title
- ✅ Creates initial problem statement message from user
- ✅ Automatically routes to Coordinator persona
- ✅ Begins the delegation chain processing

##### ProcessDelegationAsync
- ✅ Processes a delegation message to its target persona
- ✅ Validates message is a delegation type
- ✅ Continues the delegation chain

##### HandleUserClarificationAsync
- ✅ Accepts user response to clarification requests
- ✅ Finds the persona that requested clarification
- ✅ Creates user response message linked to the original question
- ✅ Resumes processing with the requesting persona
- ✅ Updates session status from WaitingForClarification to Active

##### ResumeSessionAsync
- ✅ Resumes paused or stuck sessions
- ✅ Finds last message and determines appropriate action
- ✅ Handles pending delegations
- ✅ Attempts recovery from stuck states via Coordinator

##### CancelSessionAsync
- ✅ Cancels an active session
- ✅ Updates session status to Cancelled

### 2. Delegation Chain Logic

The orchestrator implements a sophisticated delegation chain:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Delegation Chain Flow                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  User Problem → Coordinator                                              │
│       │                                                                  │
│       ▼                                                                  │
│  ┌─────────────┐                                                         │
│  │ ProcessWith │──────────────────────────────────────────────┐          │
│  │  Persona    │                                              │          │
│  └──────┬──────┘                                              │          │
│         │                                                     │          │
│         ▼                                                     │          │
│  ┌─────────────────────────────────────────────────────────┐ │          │
│  │              Response Type Router                        │ │          │
│  ├─────────────────────────────────────────────────────────┤ │          │
│  │ Solution     → Complete session, compile final solution  │ │          │
│  │ Clarification → Pause session, wait for user            │ │          │
│  │ Delegation   → Continue with target persona ────────────┼─┘          │
│  │ Decline      → Send to Coordinator for alternative      │            │
│  │ Stuck        → Track stuck, try recovery or fail        │            │
│  │ Answer       → Return to Coordinator if substantial     │            │
│  └─────────────────────────────────────────────────────────┘            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Key Delegation Rules
- ✅ Maximum delegation depth: 50 (prevents infinite loops)
- ✅ Maximum consecutive stuck: 5 (triggers session stuck state)
- ✅ Tracks which personas are stuck
- ✅ Solutions from non-Coordinators are sent back for final compilation
- ✅ Substantial answers are routed back to Coordinator

### 3. Stuck Detection Algorithm

The orchestrator detects when the session is stuck and cannot proceed:

1. **Individual Stuck Tracking**: Each persona's stuck state is recorded
2. **Consecutive Stuck Counter**: Tracks how many personas in a row are stuck
3. **Global Stuck Detection**: Triggers when:
   - 5+ consecutive personas report stuck
   - All non-User personas have reported stuck
   - Coordinator is stuck (immediate session stuck)

When stuck is detected:
- ✅ Compiles partial solution from all contributions
- ✅ Sets session status to Stuck
- ✅ Includes guidance for user on how to proceed

### 4. Partial Solution Compilation

When a session gets stuck, the orchestrator compiles a partial solution:

```markdown
# Partial Solution (Session Incomplete)

The team was unable to complete the solution, but here is what was accomplished:

**Coordinator:**
[Coordinator's analysis and plan]

**BusinessAnalyst:**
[Requirements gathered]

...

---

## What's Missing

The team encountered difficulties and was unable to proceed further. You may:
1. Provide additional clarification or context
2. Break the problem into smaller, more specific tasks
3. Try a different approach to the problem
```

### 5. Updated API Endpoints

**File:** [src/ANXAgentSwarm.Api/Controllers/SessionsController.cs](../src/ANXAgentSwarm.Api/Controllers/SessionsController.cs)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sessions` | Create new session with problem statement |
| GET | `/api/sessions` | List all sessions |
| GET | `/api/sessions/{id}` | Get session with messages |
| GET | `/api/sessions/{id}/messages` | Get all messages for session |
| POST | `/api/sessions/{id}/clarify` | Submit user clarification |
| POST | `/api/sessions/{id}/resume` | Resume paused/stuck session |
| POST | `/api/sessions/{id}/cancel` | Cancel a session |
| DELETE | `/api/sessions/{id}` | Delete a session |

### 6. Dependency Injection
**File:** [src/ANXAgentSwarm.Infrastructure/DependencyInjection.cs](../src/ANXAgentSwarm.Infrastructure/DependencyInjection.cs)

Registered services:
- ✅ `IAgentOrchestrator` → `AgentOrchestrator` (Scoped)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           AgentOrchestrator                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ ProcessWithPersonaAsync(session, message, persona)                     │ │
│  │   1. Update session's current persona                                  │ │
│  │   2. Get relevant memories for persona                                 │ │
│  │   3. Call PersonaEngine.ProcessAsync()                                 │ │
│  │   4. Create response message                                           │ │
│  │   5. Route based on response type                                      │ │
│  │   6. Continue chain or complete                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                              │                                               │
│              ┌───────────────┼───────────────┬───────────────┐              │
│              ▼               ▼               ▼               ▼              │
│       ┌───────────┐  ┌─────────────┐  ┌────────────┐  ┌───────────┐        │
│       │ Session   │  │  Message    │  │  Memory    │  │  Persona  │        │
│       │ Repository│  │  Repository │  │  Service   │  │  Engine   │        │
│       └───────────┘  └─────────────┘  └────────────┘  └───────────┘        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Session State Machine

```
                    ┌──────────────────┐
                    │                  │
          ┌────────►│     Active       │◄────────┐
          │         │                  │         │
          │         └────────┬─────────┘         │
          │                  │                   │
          │    ┌─────────────┼─────────────┐     │
          │    ▼             ▼             ▼     │
          │ Delegation   Clarification  Solution │
          │    │             │             │     │
          │    │             ▼             │     │
          │    │  ┌──────────────────┐     │     │
          │    │  │  WaitingFor      │     │     │
          │    │  │  Clarification   │─────┘     │
          │    │  └──────────────────┘           │
          │    │         ▲                       │
          │    │         │ User Response         │
          │    │         │                       │
          │    └─────────┤                       │
          │              │                       ▼
          │              │            ┌──────────────────┐
          │              │            │                  │
          │              │            │    Completed     │
          │              │            │                  │
          │              │            └──────────────────┘
          │              │
          │   Stuck      │ Resume
          │   Detection  │
          ▼              │
   ┌──────────────────┐  │
   │                  │──┘
   │      Stuck       │
   │                  │
   └──────────────────┘
          │
          │ Cancel
          ▼
   ┌──────────────────┐
   │                  │
   │    Cancelled     │
   │                  │
   └──────────────────┘
```

---

## Files Created/Modified

| File | Action | Description |
|------|--------|-------------|
| `Services/AgentOrchestrator.cs` | Created | Full orchestration logic |
| `DependencyInjection.cs` | Modified | Registered AgentOrchestrator |
| `Controllers/SessionsController.cs` | Modified | Added all session endpoints |

---

## API Usage Examples

### Create a New Session
```bash
POST /api/sessions
Content-Type: application/json

{
  "problemStatement": "Create a REST API for a todo list application with user authentication"
}
```

Response:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Create a REST API for a todo list application...",
  "status": "Active",
  "problemStatement": "Create a REST API for a todo list application with user authentication",
  "finalSolution": null,
  "createdAt": "2026-02-01T12:00:00Z",
  "updatedAt": "2026-02-01T12:00:05Z",
  "currentPersona": "BusinessAnalyst",
  "messages": [
    {
      "id": "...",
      "fromPersona": "User",
      "toPersona": "Coordinator",
      "content": "Create a REST API for a todo list application with user authentication",
      "messageType": "ProblemStatement",
      ...
    },
    {
      "id": "...",
      "fromPersona": "Coordinator",
      "toPersona": "BusinessAnalyst",
      "content": "I've analyzed the request...",
      "messageType": "Delegation",
      ...
    }
  ]
}
```

### Submit Clarification
```bash
POST /api/sessions/{id}/clarify
Content-Type: application/json

{
  "response": "I prefer JWT authentication with refresh tokens"
}
```

### Resume a Stuck Session
```bash
POST /api/sessions/{id}/resume
```

---

## Next Phase: SignalR Integration

### Phase 4 Prompt

```
Continue with Phase 4: SignalR Integration
- Create a SessionHub for real-time communication
- Emit events when messages are created (MessageReceived)
- Emit events when session status changes (SessionStatusChanged)
- Emit events for clarification requests (ClarificationRequested)
- Emit events when solution is ready (SolutionReady)
- Emit events when session is stuck (SessionStuck)
- Update the orchestrator to use the hub for broadcasting
- Update Program.cs to configure SignalR
- Provide the next prompt
- Update docs/progress-phase-4.md after completion
```

### What Phase 4 Will Include

1. **SessionHub**
   - Client connection management (JoinSession, LeaveSession)
   - Server-to-client event broadcasting
   
2. **Real-time Events**
   - `MessageReceived`: New message added to session
   - `SessionStatusChanged`: Session status update
   - `ClarificationRequested`: Persona needs user input
   - `SolutionReady`: Final solution available
   - `SessionStuck`: All personas stuck, partial results available

3. **Integration**
   - Inject IHubContext into AgentOrchestrator
   - Broadcast events during session processing
   - Group management for session rooms

---

## Testing Notes

To test the current implementation:

1. Ensure Ollama is running: `ollama serve`
2. Pull the model: `ollama pull gemma3`
3. Run the API: `dotnet run --project src/ANXAgentSwarm.Api`
4. Test endpoints:

```bash
# Create a new session
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"problemStatement": "Create a simple calculator API"}'

# Get session details
curl http://localhost:5000/api/sessions/{session-id}

# Submit clarification (when session is waiting)
curl -X POST http://localhost:5000/api/sessions/{session-id}/clarify \
  -H "Content-Type: application/json" \
  -d '{"response": "I want addition and subtraction only"}'
```
