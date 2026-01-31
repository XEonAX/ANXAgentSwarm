namespace ANXAgentSwarm.Core.Enums;

/// <summary>
/// Represents the different persona types (agents) available in the system.
/// </summary>
public enum PersonaType
{
    /// <summary>
    /// Central orchestrator - receives problems, plans approach, compiles solutions.
    /// </summary>
    Coordinator = 0,

    /// <summary>
    /// Requirements specialist - gathers requirements, analyzes business needs.
    /// </summary>
    BusinessAnalyst = 1,

    /// <summary>
    /// System design expert - architecture decisions, technology selection.
    /// </summary>
    TechnicalArchitect = 2,

    /// <summary>
    /// Expert implementer - complex features, code review, mentoring.
    /// </summary>
    SeniorDeveloper = 3,

    /// <summary>
    /// Eager learner - basic features, follows patterns, learning-oriented.
    /// </summary>
    JuniorDeveloper = 4,

    /// <summary>
    /// Quality strategy expert - test strategy, complex test cases.
    /// </summary>
    SeniorQA = 5,

    /// <summary>
    /// Test executor - test execution, bug reporting.
    /// </summary>
    JuniorQA = 6,

    /// <summary>
    /// User experience designer - user flows, wireframes, usability.
    /// </summary>
    UXEngineer = 7,

    /// <summary>
    /// Visual designer - visual interfaces, component styling.
    /// </summary>
    UIEngineer = 8,

    /// <summary>
    /// Technical writer - documentation, user guides, API docs.
    /// </summary>
    DocumentWriter = 9,

    /// <summary>
    /// Represents the user (for messages from the user).
    /// </summary>
    User = 99
}
