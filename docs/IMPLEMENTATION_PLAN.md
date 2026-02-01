# ANXAgentSwarm - Implementation Plan

## Overview

ANXAgentSwarm is an Ollama-based multi-agent system that solves problems through collaborative persona interactions. Users submit problems to a Coordinator agent, who orchestrates conversations between specialized personas (Business Analyst, Developers, QA, etc.) until a complete solution is generated.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Vue 3 + Vite + TS                               │
│                           (Real-time Timeline UI)                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ SignalR WebSocket
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ASP.NET Core Web API                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │   Session   │  │   Agent     │  │   Memory    │  │   File System       │ │
│  │   Manager   │  │ Orchestrator│  │   Service   │  │   Service           │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│                          │                                                   │
│                          ▼                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                        Persona Engine                                    ││
│  │  ┌───────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  ││
│  │  │Coordinator│ │ Business │ │Technical │ │Developers│ │   QA Team    │  ││
│  │  │           │ │ Analyst  │ │Architect │ │ Sr / Jr  │ │   Sr / Jr    │  ││
│  │  └───────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────────┘  ││
│  │  ┌───────────┐ ┌──────────┐ ┌────────────────────┐                      ││
│  │  │    UX     │ │    UI    │ │  Document Writer   │                      ││
│  │  │ Engineer  │ │ Engineer │ │                    │                      ││
│  │  └───────────┘ └──────────┘ └────────────────────┘                      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    ▼                 ▼                 ▼
            ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
            │   SQLite    │   │   Ollama    │   │  Workspace  │
            │  Database   │   │   (LLM)     │   │   Folder    │
            └─────────────┘   └─────────────┘   └─────────────┘
```

---

## Technology Stack

### Backend
- **Framework**: .NET 8 (ASP.NET Core)
- **Database**: SQLite with Entity Framework Core
- **Real-time**: SignalR
- **LLM Provider**: Ollama (configurable, default: gemma3)
- **Architecture**: Clean Architecture with CQRS-lite pattern

### Frontend
- **Framework**: Vue 3 (Composition API)
- **Build Tool**: Vite
- **Language**: TypeScript
- **UI Library**: Tailwind CSS + Headless UI
- **Real-time**: SignalR Client

### Testing
- **Backend**: xUnit + Moq + FluentAssertions
- **Frontend**: Vitest + Vue Test Utils
- **Coverage Target**: 100%

---

## Project Structure

```
ANXAgentSwarm/
├── src/
│   ├── ANXAgentSwarm.Api/                 # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Hubs/                          # SignalR Hubs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── ANXAgentSwarm.Core/                # Domain & Business Logic
│   │   ├── Entities/
│   │   │   ├── Session.cs
│   │   │   ├── Message.cs
│   │   │   ├── Persona.cs
│   │   │   └── Memory.cs
│   │   ├── Enums/
│   │   │   ├── PersonaType.cs
│   │   │   ├── MessageType.cs
│   │   │   └── SessionStatus.cs
│   │   ├── Interfaces/
│   │   │   ├── ILlmProvider.cs
│   │   │   ├── IPersonaEngine.cs
│   │   │   ├── IMemoryService.cs
│   │   │   └── ISessionRepository.cs
│   │   └── Services/
│   │       ├── AgentOrchestrator.cs
│   │       ├── PersonaEngine.cs
│   │       └── MemoryService.cs
│   │
│   ├── ANXAgentSwarm.Infrastructure/      # Data Access & External Services
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   ├── LlmProviders/
│   │   │   └── OllamaProvider.cs
│   │   └── FileSystem/
│   │       └── WorkspaceService.cs
│   │
│   └── ANXAgentSwarm.Web/                 # Vue 3 Frontend
│       ├── src/
│       │   ├── components/
│       │   │   ├── Timeline.vue
│       │   │   ├── MessageCard.vue
│       │   │   ├── SessionList.vue
│       │   │   └── UserInput.vue
│       │   ├── composables/
│       │   │   ├── useSession.ts
│       │   │   └── useSignalR.ts
│       │   ├── stores/
│       │   │   └── sessionStore.ts
│       │   ├── types/
│       │   │   └── index.ts
│       │   ├── App.vue
│       │   └── main.ts
│       ├── index.html
│       ├── vite.config.ts
│       ├── tsconfig.json
│       └── package.json
│
├── tests/
│   ├── ANXAgentSwarm.Core.Tests/
│   ├── ANXAgentSwarm.Infrastructure.Tests/
│   ├── ANXAgentSwarm.Api.Tests/
│   └── ANXAgentSwarm.Web.Tests/
│
├── workspace/                             # Shared file access folder
│
├── docs/
│   ├── IMPLEMENTATION_PLAN.md
│   └── agents.md
│
├── .github/
│   └── copilot-instructions.md
│
├── ANXAgentSwarm.sln
└── README.md
```

---

## Database Schema

### Tables

#### Sessions
```sql
CREATE TABLE Sessions (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Status INTEGER NOT NULL,           -- 0=Active, 1=Completed, 2=Stuck
    ProblemStatement TEXT NOT NULL,
    FinalSolution TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

#### Messages
```sql
CREATE TABLE Messages (
    Id TEXT PRIMARY KEY,
    SessionId TEXT NOT NULL,
    FromPersona INTEGER NOT NULL,      -- PersonaType enum
    ToPersona INTEGER,                 -- NULL if to user
    Content TEXT NOT NULL,
    MessageType INTEGER NOT NULL,      -- 0=Question, 1=Answer, 2=Delegation, 3=Clarification, 4=Solution
    InternalReasoning TEXT,            -- Full thinking process
    Timestamp TEXT NOT NULL,
    ParentMessageId TEXT,              -- For threading
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id),
    FOREIGN KEY (ParentMessageId) REFERENCES Messages(Id)
);
```

#### Memories
```sql
CREATE TABLE Memories (
    Id TEXT PRIMARY KEY,
    SessionId TEXT NOT NULL,
    PersonaType INTEGER NOT NULL,
    Identifier TEXT NOT NULL,          -- 10-word summary
    Content TEXT NOT NULL,             -- Up to 2000 words
    CreatedAt TEXT NOT NULL,
    AccessCount INTEGER DEFAULT 0,
    LastAccessedAt TEXT,
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id)
);
```

#### PersonaConfigurations
```sql
CREATE TABLE PersonaConfigurations (
    Id TEXT PRIMARY KEY,
    PersonaType INTEGER NOT NULL UNIQUE,
    ModelName TEXT NOT NULL DEFAULT 'gemma3',
    SystemPrompt TEXT NOT NULL,
    Temperature REAL DEFAULT 0.7,
    MaxTokens INTEGER DEFAULT 4096
);
```

---

## Core Components

### 1. Agent Orchestrator

The central coordinator that:
- Receives user problems
- Routes to Coordinator persona first
- Manages sequential delegation between personas
- Detects stuck states (all personas say "I don't know")
- Broadcasts real-time updates via SignalR

```csharp
public interface IAgentOrchestrator
{
    Task<Session> StartSessionAsync(string problemStatement);
    Task<Message> ProcessDelegationAsync(Guid sessionId, Guid messageId);
    Task<Message> HandleUserClarificationAsync(Guid sessionId, string response);
    Task<Session> ResumeSessionAsync(Guid sessionId);
}
```

### 2. Persona Engine

Executes persona logic:
- Loads persona-specific system prompts
- Manages memory retrieval and storage
- Generates responses via LLM
- Determines next action (answer, delegate, clarify, stuck)

```csharp
public interface IPersonaEngine
{
    Task<PersonaResponse> ProcessAsync(
        PersonaType persona,
        Message incomingMessage,
        Session session,
        IEnumerable<Memory> relevantMemories
    );
}

public class PersonaResponse
{
    public string Content { get; set; }
    public string InternalReasoning { get; set; }
    public MessageType ResponseType { get; set; }
    public PersonaType? DelegateToPersona { get; set; }
    public string? DelegationContext { get; set; }
    public bool IsStuck { get; set; }
    public Memory? NewMemory { get; set; }
}
```

### 3. Memory Service

Handles persona memory within sessions:
- Store memories (max 2000 words, 10-word identifier)
- Search memories by keyword
- List last 10 memories per persona per session
- Automatic relevance retrieval

```csharp
public interface IMemoryService
{
    Task<Memory> StoreMemoryAsync(Guid sessionId, PersonaType persona, string identifier, string content);
    Task<IEnumerable<Memory>> SearchMemoriesAsync(Guid sessionId, PersonaType persona, string query);
    Task<IEnumerable<Memory>> GetRecentMemoriesAsync(Guid sessionId, PersonaType persona, int count = 10);
    Task<Memory?> GetMemoryByIdentifierAsync(Guid sessionId, PersonaType persona, string identifier);
}
```

### 4. LLM Provider

Abstraction for LLM communication:

```csharp
public interface ILlmProvider
{
    Task<LlmResponse> GenerateAsync(LlmRequest request);
    Task<bool> IsAvailableAsync();
}

public class LlmRequest
{
    public string Model { get; set; } = "gemma3:27b";
    public string SystemPrompt { get; set; }
    public List<ChatMessage> Messages { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}
```

### 5. Workspace Service

File system access within the designated folder:

```csharp
public interface IWorkspaceService
{
    Task<string> ReadFileAsync(string relativePath);
    Task WriteFileAsync(string relativePath, string content);
    Task<IEnumerable<string>> ListFilesAsync(string relativePath = "");
    Task<bool> FileExistsAsync(string relativePath);
    Task DeleteFileAsync(string relativePath);
}
```

---

## API Endpoints

### Sessions
```
POST   /api/sessions                    # Create new session with problem statement
GET    /api/sessions                    # List all sessions
GET    /api/sessions/{id}               # Get session details
PUT    /api/sessions/{id}/resume        # Resume a paused session
DELETE /api/sessions/{id}               # Delete session
```

### Messages
```
GET    /api/sessions/{id}/messages      # Get all messages in session
POST   /api/sessions/{id}/clarify       # Submit user clarification response
```

### Workspace
```
GET    /api/workspace/files             # List files
GET    /api/workspace/files/{path}      # Read file
PUT    /api/workspace/files/{path}      # Write file
DELETE /api/workspace/files/{path}      # Delete file
```

---

## SignalR Hub

```csharp
public class SessionHub : Hub
{
    // Client joins session room
    Task JoinSession(string sessionId);
    
    // Client leaves session room
    Task LeaveSession(string sessionId);
}

// Server-to-Client Events:
// - "MessageReceived" (Message)
// - "SessionStatusChanged" (SessionStatus)
// - "ClarificationRequested" (Message)
// - "SolutionReady" (Session)
// - "SessionStuck" (Session, partialResults)
```

---

## Configuration

### appsettings.json
```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "gemma3:27b",
    "TimeoutSeconds": 120
  },
  "Personas": {
    "Coordinator": { "Model": "gemma3:27b", "Temperature": 0.7 },
    "BusinessAnalyst": { "Model": "gemma3:27b", "Temperature": 0.7 },
    "TechnicalArchitect": { "Model": "gemma3:27b", "Temperature": 0.6 },
    "SeniorDeveloper": { "Model": "gemma3:27b", "Temperature": 0.5 },
    "JuniorDeveloper": { "Model": "gemma3:27b", "Temperature": 0.6 },
    "SeniorQA": { "Model": "gemma3:27b", "Temperature": 0.5 },
    "JuniorQA": { "Model": "gemma3:27b", "Temperature": 0.6 },
    "UXEngineer": { "Model": "gemma3:27b", "Temperature": 0.7 },
    "UIEngineer": { "Model": "gemma3:27b", "Temperature": 0.6 },
    "DocumentWriter": { "Model": "gemma3:27b", "Temperature": 0.7 }
  },
  "Memory": {
    "MaxWordsPerMemory": 2000,
    "MaxIdentifierWords": 10,
    "MaxMemoriesPerPersonaPerSession": 10
  },
  "Workspace": {
    "RootPath": "./workspace"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=anxagentswarm.db"
  }
}
```

---

## Conversation Flow

### Normal Flow
```
User → Problem Statement
         ↓
    Coordinator (analyzes, plans)
         ↓
    Delegates to Business Analyst
         ↓
    BA analyzes, delegates to Technical Architect
         ↓
    TA designs, delegates to Senior Developer
         ↓
    Sr Dev implements, asks Jr Dev for subtask
         ↓
    Jr Dev completes, returns to Sr Dev
         ↓
    Sr Dev → QA for review
         ↓
    QA validates → Document Writer
         ↓
    Doc Writer creates docs → Coordinator
         ↓
    Coordinator compiles final solution
         ↓
    User ← Complete Solution
```

### Clarification Flow
```
User → Problem Statement
         ↓
    Coordinator → Business Analyst
         ↓
    BA needs clarification
         ↓
    [PAUSE - Wait for User]
         ↓
    User provides clarification
         ↓
    BA continues processing
         ↓
    ... (normal flow continues)
```

### Stuck Flow
```
User → Problem Statement
         ↓
    Coordinator → BA → TA → Sr Dev
         ↓
    Sr Dev: "I don't know how to proceed"
         ↓
    Coordinator tries Jr Dev, QA, etc.
         ↓
    All personas: "I don't know"
         ↓
    Return partial results + ask user for guidance
```

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Project scaffolding (.NET solution, Vue project)
- [ ] SQLite database setup with EF Core
- [ ] Basic entity models and repositories
- [ ] Ollama provider implementation
- [ ] Configuration system

### Phase 2: Persona Engine (Week 2)
- [ ] Persona type definitions and prompts
- [ ] Memory service implementation
- [ ] Persona engine with LLM integration
- [ ] Response parsing (delegation, clarification, solution)

### Phase 3: Orchestration (Week 3)
- [ ] Agent orchestrator implementation
- [ ] Sequential delegation logic
- [ ] Stuck detection algorithm
- [ ] Clarification pause/resume flow
- [ ] SignalR hub setup

### Phase 4: API Layer (Week 4)
- [ ] Session controller
- [ ] Message controller
- [ ] Workspace controller
- [ ] Error handling middleware
- [ ] Request validation

### Phase 5: Frontend (Week 5)
- [ ] Vue project setup with Vite
- [ ] SignalR client integration
- [ ] Session list component
- [ ] Timeline component
- [ ] Message card component
- [ ] User input component
- [ ] Clarification handling

### Phase 6: Testing (Week 6)
- [ ] Unit tests for Core (100% coverage)
- [ ] Unit tests for Infrastructure (100% coverage)
- [ ] Integration tests for API
- [ ] Component tests for Vue
- [ ] End-to-end flow tests

### Phase 7: Polish (Week 7)
- [ ] Error handling improvements
- [ ] Performance optimization
- [ ] Documentation
- [ ] README with setup instructions

---

## Testing Strategy

### Unit Tests
- **Core Layer**: All services, entities, and business logic
- **Infrastructure Layer**: Repository methods, LLM provider mocking
- **API Layer**: Controller actions with mocked services

### Integration Tests
- Database operations with in-memory SQLite
- Full orchestration flows
- SignalR hub connections

### Frontend Tests
- Component rendering tests
- Store/composable logic tests
- SignalR mock integration tests

### Coverage Requirements
- Minimum: 100% line coverage
- All branches covered
- All edge cases documented

---

## Success Criteria

1. ✅ User can submit a problem and receive a collaborative solution
2. ✅ All persona communications visible in real-time timeline
3. ✅ Personas can delegate, ask questions, and respond appropriately
4. ✅ System handles stuck states gracefully
5. ✅ User can provide clarifications when requested
6. ✅ Sessions are persisted and resumable
7. ✅ Memory system works within constraints
8. ✅ 100% test coverage achieved
9. ✅ LLM provider is configurable per persona
10. ✅ Workspace folder access works correctly
