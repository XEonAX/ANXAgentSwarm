# ANXAgentSwarm

<div align="center">

![ANXAgentSwarm](https://img.shields.io/badge/ANXAgentSwarm-v1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Vue](https://img.shields.io/badge/Vue-3.5-42b883)
![License](https://img.shields.io/badge/License-MIT-green)

**An Ollama-based multi-agent system for collaborative problem-solving**

[Features](#features) • [Quick Start](#quick-start) • [Architecture](#architecture) • [Documentation](#documentation) • [Contributing](#contributing)

</div>

---

## Overview

ANXAgentSwarm is an intelligent multi-agent orchestration system that leverages Ollama-powered LLMs to solve complex problems through collaborative AI persona interactions. Submit a problem, and watch as specialized agents—from Business Analysts to Technical Architects to Developers—work together to provide comprehensive solutions.

## Features

- 🤖 **10 Specialized AI Personas** - Each with unique expertise and communication styles
- 🔄 **Real-time Collaboration** - Watch agents interact via SignalR live updates
- 💬 **Interactive Clarifications** - Agents can ask users for additional information
- 🧠 **Session Memory** - Persistent memory system for context retention
- 📁 **Workspace Integration** - Agents can create and manage files
- 🎨 **Modern Vue 3 UI** - Beautiful, responsive interface with Tailwind CSS
- 🐳 **Docker Ready** - Full containerized deployment support
- ✅ **100% Test Coverage** - Comprehensive unit and integration tests

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org/)
- [Ollama](https://ollama.ai/) with a model installed (default: `gemma3`)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/ANXAgentSwarm.git
cd ANXAgentSwarm

# Start Ollama and pull the default model
ollama serve
ollama pull gemma3

# Start the development servers
./scripts/dev.sh
```

The application will be available at:
- **Frontend**: http://localhost:5173
- **API**: http://localhost:5046

### Docker Deployment

```bash
# Build and start all services
./scripts/deploy-docker.sh build
./scripts/deploy-docker.sh start

# Or use docker-compose directly
docker-compose up -d
```

Access at http://localhost

## Architecture

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
└─────────────────────────────────────────────────────────────────────────────┘
                    │                 │                 │
                    ▼                 ▼                 ▼
            ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
            │   SQLite    │   │   Ollama    │   │  Workspace  │
            │  Database   │   │   (LLM)     │   │   Folder    │
            └─────────────┘   └─────────────┘   └─────────────┘
```

### Project Structure

```
ANXAgentSwarm/
├── src/
│   ├── ANXAgentSwarm.Api/         # ASP.NET Core Web API
│   ├── ANXAgentSwarm.Core/        # Domain & Business Logic
│   ├── ANXAgentSwarm.Infrastructure/  # Data Access & External Services
│   └── ANXAgentSwarm.Web/         # Vue 3 Frontend
├── tests/
│   ├── ANXAgentSwarm.Infrastructure.Tests/
│   └── ANXAgentSwarm.Integration.Tests/
├── scripts/                       # Build & deployment scripts
├── docker/                        # Docker configuration
├── docs/                          # Documentation
└── workspace/                     # Shared file access folder
```

## AI Personas

| Persona | Role | Expertise |
|---------|------|-----------|
| 🎯 Coordinator | Orchestrator | Problem analysis, delegation, solution compilation |
| 📊 Business Analyst | Requirements | Business logic, user stories, stakeholder analysis |
| 🏗️ Technical Architect | Design | System architecture, technology selection |
| 👨‍💻 Senior Developer | Implementation | Complex features, code review, mentoring |
| 👩‍💻 Junior Developer | Implementation | Basic features, following patterns |
| 🔍 Senior QA | Quality | Test strategy, complex test scenarios |
| 🧪 Junior QA | Quality | Test execution, bug reporting |
| 🎨 UX Engineer | Experience | User flows, wireframes, usability |
| 🖌️ UI Engineer | Interface | Visual design, components, styling |
| 📝 Document Writer | Documentation | Technical writing, API docs, guides |

## API Reference

### Sessions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sessions` | Create a new session |
| GET | `/api/sessions` | List all sessions |
| GET | `/api/sessions/{id}` | Get session details |
| POST | `/api/sessions/{id}/clarify` | Submit clarification |
| POST | `/api/sessions/{id}/resume` | Resume paused session |
| POST | `/api/sessions/{id}/cancel` | Cancel session |
| DELETE | `/api/sessions/{id}` | Delete session |

### Status

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Basic health check |
| GET | `/api/status/health` | Detailed health status |
| GET | `/api/status/llm` | LLM provider status |

### SignalR Events

| Event | Direction | Description |
|-------|-----------|-------------|
| `MessageReceived` | Server → Client | New message in session |
| `SessionStatusChanged` | Server → Client | Session status update |
| `ClarificationRequested` | Server → Client | Persona needs input |
| `SolutionReady` | Server → Client | Solution available |
| `JoinSession` | Client → Server | Subscribe to session |
| `LeaveSession` | Client → Server | Unsubscribe |

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Development` |
| `OLLAMA_BASE_URL` | Ollama server URL | `http://localhost:11434` |
| `OLLAMA_MODEL` | Default LLM model | `gemma3` |
| `OLLAMA_TIMEOUT` | Request timeout (seconds) | `120` |
| `DATABASE_CONNECTION` | SQLite connection string | `Data Source=./anxagentswarm.db` |
| `WORKSPACE_ROOT` | Workspace folder path | `./workspace` |

See [.env.example](.env.example) for all available options.

## Development

### Running Tests

```bash
# Run all .NET tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run E2E tests
cd src/ANXAgentSwarm.Web
npm run test:e2e

# Run with Playwright UI
npm run test:e2e:ui

# Run accessibility tests
npm run test:a11y

# Run visual regression tests
npm run test:visual
```

### Building for Production

```bash
# Build all components
./scripts/build.sh

# Build Docker images
./scripts/build.sh docker

# Deploy with Docker
./scripts/deploy-docker.sh start
```

## Documentation

- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md) - Detailed technical specification
- [Persona Definitions](agents.md) - Complete persona documentation
- [Copilot Instructions](.github/copilot-instructions.md) - AI assistant guidelines

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style

- **C#**: Follow .NET conventions, use async/await, dependency injection
- **TypeScript**: Strict mode, Vue 3 Composition API with `<script setup>`
- **Testing**: 100% coverage target, meaningful test names

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Ollama](https://ollama.ai/) - Local LLM runtime
- [Vue.js](https://vuejs.org/) - Progressive JavaScript framework
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/) - Web framework
- [Tailwind CSS](https://tailwindcss.com/) - Utility-first CSS

---

<div align="center">

Made with ❤️ by the ANXAgentSwarm Team

</div>
