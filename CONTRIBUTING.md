# Contributing to ANXAgentSwarm

First off, thank you for considering contributing to ANXAgentSwarm! It's people like you that make this project better for everyone. 🎉

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)
- [Community](#community)

## Code of Conduct

This project and everyone participating in it is governed by our commitment to creating a welcoming and inclusive environment. We expect all contributors to:

- Be respectful and inclusive
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org/)
- [Ollama](https://ollama.ai/)
- [Git](https://git-scm.com/)
- [Docker](https://www.docker.com/) (optional, for containerized development)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/ANXAgentSwarm.git
   cd ANXAgentSwarm
   ```
3. Add the upstream repository:
   ```bash
   git remote add upstream https://github.com/yourusername/ANXAgentSwarm.git
   ```

## Development Setup

### Quick Start

```bash
# Install dependencies
dotnet restore
cd src/ANXAgentSwarm.Web && npm install && cd ../..

# Start Ollama
ollama serve
ollama pull gemma3

# Run the development servers
./scripts/dev.sh
```

### Manual Setup

**Backend:**
```bash
# Start the API
cd src/ANXAgentSwarm.Api
dotnet run
```

**Frontend:**
```bash
# Start the Vue dev server
cd src/ANXAgentSwarm.Web
npm run dev
```

### Docker Development

```bash
docker-compose -f docker-compose.dev.yml up
```

## How to Contribute

### Types of Contributions

We welcome many types of contributions:

- 🐛 **Bug Reports** - Help us identify and fix issues
- ✨ **Feature Requests** - Suggest new ideas and features
- 📖 **Documentation** - Improve our docs, guides, and examples
- 🧪 **Tests** - Add or improve test coverage
- 💻 **Code** - Fix bugs or implement features
- 🎨 **Design** - UI/UX improvements
- 🌍 **Translations** - Help make the project accessible globally

### Finding Something to Work On

- Look for issues labeled [`good first issue`](https://github.com/yourusername/ANXAgentSwarm/labels/good%20first%20issue)
- Check [`help wanted`](https://github.com/yourusername/ANXAgentSwarm/labels/help%20wanted) issues
- Review the [project roadmap](https://github.com/yourusername/ANXAgentSwarm/projects)

### Before Starting Work

1. Check if an issue exists for your planned work
2. If not, create one to discuss the approach
3. Wait for feedback before starting significant work
4. Assign yourself to the issue

## Coding Standards

### C# (.NET)

Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

```csharp
// Use file-scoped namespaces
namespace ANXAgentSwarm.Core.Entities;

// Use primary constructors for simple classes
public class Memory(Guid id, string identifier, string content)
{
    public Guid Id { get; } = id;
}

// Use async/await consistently
public async Task<Session> GetSessionAsync(Guid id)
{
    return await _context.Sessions.FindAsync(id) 
        ?? throw new NotFoundException($"Session {id} not found");
}

// Use records for DTOs
public record CreateSessionRequest(string ProblemStatement);
```

**Key Points:**
- Use `async/await` for all I/O operations
- Enable nullable reference types
- Use dependency injection
- Follow the repository pattern
- Write interface-first

### TypeScript/Vue

```typescript
// Use Composition API with script setup
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { Message } from '@/types'

// Props with TypeScript
const props = defineProps<{
  sessionId: string
}>()

// Reactive state
const messages = ref<Message[]>([])

// Lifecycle hooks
onMounted(async () => {
  await loadMessages()
})
</script>
```

**Key Points:**
- Use TypeScript strict mode
- Use Composition API with `<script setup>`
- Define proper types (no `any`)
- Use Pinia for state management

### Git Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(personas): add new DevOps Engineer persona
fix(signalr): resolve reconnection loop issue
docs(readme): update installation instructions
test(orchestrator): add delegation tests
```

## Testing Guidelines

### Test Coverage

We aim for 100% test coverage. All new code should include tests.

### Running Tests

```bash
# Backend tests
dotnet test

# E2E tests
cd src/ANXAgentSwarm.Web
npm run test:e2e
```

### Writing Tests

**Unit Tests (Backend):**
```csharp
[Fact]
public async Task ProcessAsync_WithDelegation_ReturnsDelegationResponse()
{
    // Arrange
    var message = CreateTestMessage();
    _llmProviderMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>()))
        .ReturnsAsync(new LlmResponse { Content = "[DELEGATE:TechnicalArchitect]" });
    
    // Act
    var result = await _sut.ProcessAsync(PersonaType.BusinessAnalyst, message);
    
    // Assert
    result.ResponseType.Should().Be(MessageType.Delegation);
}
```

**E2E Tests:**
```typescript
test('should create new session', async ({ page }) => {
  await page.goto('/');
  await page.fill('[data-testid="problem-input"]', 'Test problem');
  await page.click('[data-testid="submit-button"]');
  await expect(page.locator('[data-testid="session-timeline"]')).toBeVisible();
});
```

## Pull Request Process

### Before Submitting

1. ✅ Ensure all tests pass: `dotnet test` and `npm run test:e2e`
2. ✅ Update documentation if needed
3. ✅ Add tests for new functionality
4. ✅ Follow coding standards
5. ✅ Rebase on latest `main`

### PR Template

Your PR should include:
- Clear description of changes
- Related issue number(s)
- Screenshots for UI changes
- Test coverage information

### Review Process

1. Submit your PR
2. Automated checks will run (CI, tests, linting)
3. A maintainer will review your code
4. Address any feedback
5. Once approved, your PR will be merged

### After Merge

- Delete your feature branch
- Update your local main branch
- Celebrate! 🎉

## Issue Guidelines

### Bug Reports

Include:
- Clear title and description
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, browser, versions)
- Screenshots if applicable
- Logs if relevant

### Feature Requests

Include:
- Clear title and description
- Use case / motivation
- Proposed solution
- Alternatives considered
- Any relevant examples

## Project Structure

```
ANXAgentSwarm/
├── src/
│   ├── ANXAgentSwarm.Api/        # ASP.NET Core Web API
│   ├── ANXAgentSwarm.Core/       # Domain entities & interfaces
│   ├── ANXAgentSwarm.Infrastructure/  # Data access & services
│   └── ANXAgentSwarm.Web/        # Vue 3 frontend
├── tests/
│   ├── ANXAgentSwarm.Infrastructure.Tests/
│   └── ANXAgentSwarm.Integration.Tests/
├── docs/                          # Documentation
├── scripts/                       # Build & deployment scripts
└── workspace/                     # Agent workspace directory
```

## Community

- **Discussions**: Use [GitHub Discussions](https://github.com/yourusername/ANXAgentSwarm/discussions)
- **Issues**: [GitHub Issues](https://github.com/yourusername/ANXAgentSwarm/issues)

## Recognition

Contributors will be:
- Listed in our README
- Mentioned in release notes
- Part of the growing ANXAgentSwarm community

---

Thank you for contributing! 💜
