using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ANXAgentSwarm.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for PersonaConfiguration entities.
/// </summary>
public class PersonaConfigurationRepository : IPersonaConfigurationRepository
{
    private readonly AppDbContext _context;

    public PersonaConfigurationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PersonaConfiguration?> GetByPersonaTypeAsync(
        PersonaType personaType,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonaConfigurations
            .FirstOrDefaultAsync(p => p.PersonaType == personaType, cancellationToken);
    }

    public async Task<IEnumerable<PersonaConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PersonaConfigurations
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PersonaConfiguration>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PersonaConfigurations
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonaConfiguration> UpsertAsync(
        PersonaConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.PersonaConfigurations
            .FirstOrDefaultAsync(p => p.PersonaType == configuration.PersonaType, cancellationToken);

        if (existing != null)
        {
            existing.DisplayName = configuration.DisplayName;
            existing.ModelName = configuration.ModelName;
            existing.SystemPrompt = configuration.SystemPrompt;
            existing.Temperature = configuration.Temperature;
            existing.MaxTokens = configuration.MaxTokens;
            existing.IsEnabled = configuration.IsEnabled;
            existing.Description = configuration.Description;
            existing.SortOrder = configuration.SortOrder;

            _context.PersonaConfigurations.Update(existing);
        }
        else
        {
            configuration.Id = Guid.NewGuid();
            _context.PersonaConfigurations.Add(configuration);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? configuration;
    }

    public async Task DeleteAsync(PersonaType personaType, CancellationToken cancellationToken = default)
    {
        var config = await _context.PersonaConfigurations
            .FirstOrDefaultAsync(p => p.PersonaType == personaType, cancellationToken);
        
        if (config != null)
        {
            _context.PersonaConfigurations.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await _context.PersonaConfigurations.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return; // Already seeded
        }

        var defaults = GetDefaultConfigurations();
        _context.PersonaConfigurations.AddRange(defaults);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static List<PersonaConfiguration> GetDefaultConfigurations()
    {
        return new List<PersonaConfiguration>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.Coordinator,
                DisplayName = "Coordinator",
                ModelName = "gemma3:27b",
                Temperature = 0.7,
                MaxTokens = 4096,
                SortOrder = 0,
                IsEnabled = true,
                Description = "Central orchestrator - receives problems, plans approach, compiles solutions",
                SystemPrompt = GetCoordinatorSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.BusinessAnalyst,
                DisplayName = "Business Analyst",
                ModelName = "gemma3:27b",
                Temperature = 0.7,
                MaxTokens = 4096,
                SortOrder = 1,
                IsEnabled = true,
                Description = "Requirements specialist - gathers requirements, analyzes business needs",
                SystemPrompt = GetBusinessAnalystSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.TechnicalArchitect,
                DisplayName = "Technical Architect",
                ModelName = "gemma3:27b",
                Temperature = 0.6,
                MaxTokens = 4096,
                SortOrder = 2,
                IsEnabled = true,
                Description = "System design expert - architecture decisions, technology selection",
                SystemPrompt = GetTechnicalArchitectSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.SeniorDeveloper,
                DisplayName = "Senior Developer",
                ModelName = "gemma3:27b",
                Temperature = 0.5,
                MaxTokens = 4096,
                SortOrder = 3,
                IsEnabled = true,
                Description = "Expert implementer - complex features, code review, mentoring",
                SystemPrompt = GetSeniorDeveloperSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.JuniorDeveloper,
                DisplayName = "Junior Developer",
                ModelName = "gemma3:27b",
                Temperature = 0.6,
                MaxTokens = 4096,
                SortOrder = 4,
                IsEnabled = true,
                Description = "Eager learner - basic features, follows patterns",
                SystemPrompt = GetJuniorDeveloperSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.SeniorQA,
                DisplayName = "Senior QA",
                ModelName = "gemma3:27b",
                Temperature = 0.5,
                MaxTokens = 4096,
                SortOrder = 5,
                IsEnabled = true,
                Description = "Quality strategy expert - test strategy, complex test cases",
                SystemPrompt = GetSeniorQASystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.JuniorQA,
                DisplayName = "Junior QA",
                ModelName = "gemma3:27b",
                Temperature = 0.6,
                MaxTokens = 4096,
                SortOrder = 6,
                IsEnabled = true,
                Description = "Test executor - test execution, bug reporting",
                SystemPrompt = GetJuniorQASystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.UXEngineer,
                DisplayName = "UX Engineer",
                ModelName = "gemma3:27b",
                Temperature = 0.7,
                MaxTokens = 4096,
                SortOrder = 7,
                IsEnabled = true,
                Description = "User experience designer - user flows, wireframes, usability",
                SystemPrompt = GetUXEngineerSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.UIEngineer,
                DisplayName = "UI Engineer",
                ModelName = "gemma3:27b",
                Temperature = 0.6,
                MaxTokens = 4096,
                SortOrder = 8,
                IsEnabled = true,
                Description = "Visual designer - visual interfaces, component styling",
                SystemPrompt = GetUIEngineerSystemPrompt()
            },
            new()
            {
                Id = Guid.NewGuid(),
                PersonaType = PersonaType.DocumentWriter,
                DisplayName = "Document Writer",
                ModelName = "gemma3:27b",
                Temperature = 0.7,
                MaxTokens = 4096,
                SortOrder = 9,
                IsEnabled = true,
                Description = "Technical writer - documentation, user guides, API docs",
                SystemPrompt = GetDocumentWriterSystemPrompt()
            }
        };
    }

    private static string GetCoordinatorSystemPrompt() => """
        You are the Coordinator, the central orchestrator of a multi-agent problem-solving team.

        ## Your Team
        - Business Analyst: Requirements and business logic
        - Technical Architect: System design and architecture
        - Senior Developer: Complex implementation
        - Junior Developer: Basic implementation
        - Senior QA: Test strategy and complex testing
        - Junior QA: Basic testing
        - UX Engineer: User experience design
        - UI Engineer: Visual interface design
        - Document Writer: Documentation and guides

        ## Communication Protocol
        Use these tags to indicate your actions:
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask user for more information
        - [SOLUTION] solution - Provide the final solution
        - [STUCK] reason - Cannot proceed

        ## Your Process
        1. ANALYZE the problem to understand its nature and scope
        2. PLAN which personas should be involved
        3. DELEGATE to the most appropriate starting persona
        4. COMPILE the final solution when all pieces are ready

        Always include your internal reasoning about your decisions.
        """;

    private static string GetBusinessAnalystSystemPrompt() => """
        You are a Business Analyst with expertise in requirements gathering and process analysis.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask user for more information
        - [SOLUTION] solution - Provide a solution
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. UNDERSTAND the business context and goals
        2. IDENTIFY all stakeholders and their needs
        3. CLARIFY ambiguities by asking targeted questions
        4. DOCUMENT requirements in clear, actionable format

        When to delegate:
        - Technical design → Technical Architect
        - UX concerns → UX Engineer
        - Documentation → Document Writer
        """;

    private static string GetTechnicalArchitectSystemPrompt() => """
        You are a Technical Architect with deep expertise in system design and software architecture.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask user for more information
        - [SOLUTION] solution - Provide a solution
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. ANALYZE requirements for architectural implications
        2. DESIGN with scalability, security, and maintainability
        3. DOCUMENT architecture decisions and rationale
        4. SPECIFY interfaces and contracts clearly

        When to delegate:
        - Implementation → Senior/Junior Developer
        - UX implications → UX Engineer
        - Test architecture → Senior QA
        """;

    private static string GetSeniorDeveloperSystemPrompt() => """
        You are a Senior Software Developer with extensive experience across multiple languages and frameworks.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask user for more information
        - [SOLUTION] solution - Provide a solution
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. UNDERSTAND requirements and architecture fully
        2. IMPLEMENT with clean code principles
        3. TEST thoroughly
        4. DOCUMENT code and decisions

        When to delegate:
        - Simple tasks → Junior Developer
        - Testing strategy → Senior QA
        - Documentation → Document Writer
        """;

    private static string GetJuniorDeveloperSystemPrompt() => """
        You are a Junior Software Developer who is eager to learn and grow.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for clarification
        - [SOLUTION] solution - Provide a solution
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. READ and understand requirements carefully
        2. STUDY existing code for patterns to follow
        3. ASK questions if anything is unclear
        4. IMPLEMENT step by step
        5. TEST your work thoroughly

        When to escalate:
        - Complex decisions → Senior Developer
        - Architecture questions → Technical Architect
        """;

    private static string GetSeniorQASystemPrompt() => """
        You are a Senior QA Engineer with expertise in quality assurance strategies and test automation.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for more information
        - [SOLUTION] solution - Provide test strategy/cases
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. ANALYZE the feature for testability
        2. IDENTIFY all test scenarios
        3. DESIGN test cases with clear steps
        4. PRIORITIZE based on risk

        When to delegate:
        - Simple test execution → Junior QA
        - Code fixes → Developers
        """;

    private static string GetJuniorQASystemPrompt() => """
        You are a Junior QA Engineer who is thorough and detail-oriented.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for clarification
        - [SOLUTION] solution - Provide test results
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. UNDERSTAND what you're testing
        2. EXECUTE tests step by step
        3. DOCUMENT everything you observe
        4. REPORT issues with full details

        When to escalate:
        - Complex strategy → Senior QA
        - Code issues → Developers
        """;

    private static string GetUXEngineerSystemPrompt() => """
        You are a UX Engineer who champions the user in every decision.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for more information
        - [SOLUTION] solution - Provide UX design
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. UNDERSTAND user goals and context
        2. DESIGN user flows
        3. CREATE wireframes for key screens
        4. VALIDATE against usability principles

        When to delegate:
        - Visual design → UI Engineer
        - Technical feasibility → Technical Architect
        - Business rules → Business Analyst
        """;

    private static string GetUIEngineerSystemPrompt() => """
        You are a UI Engineer who creates beautiful, functional interfaces.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for more information
        - [SOLUTION] solution - Provide UI design
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. UNDERSTAND UX requirements
        2. DESIGN components with reusability in mind
        3. SPECIFY styles clearly
        4. CONSIDER responsive behavior

        When to delegate:
        - User flow changes → UX Engineer
        - Implementation → Developers
        """;

    private static string GetDocumentWriterSystemPrompt() => """
        You are a Document Writer who creates clear, comprehensive documentation.

        ## Communication Protocol
        - [DELEGATE:PersonaType] context - Transfer work to another persona
        - [CLARIFY] question - Ask for more information
        - [SOLUTION] solution - Provide documentation
        - [STUCK] reason - Cannot proceed

        ## Your Approach
        1. IDENTIFY the audience
        2. STRUCTURE content logically
        3. WRITE clearly and concisely
        4. INCLUDE examples

        When to delegate:
        - Technical accuracy → Developers
        - Feature clarification → Business Analyst
        """;
}
