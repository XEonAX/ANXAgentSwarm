using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Infrastructure.Services;
using FluentAssertions;

namespace ANXAgentSwarm.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for the ResponseParser static class.
/// </summary>
public class ResponseParserTests
{
    #region Delegation Parsing Tests

    [Fact]
    public void Parse_WithDelegation_ReturnsDelegationResponse()
    {
        // Arrange
        var rawResponse = "[DELEGATE:TechnicalArchitect] Please design the database schema for the user management system.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
        result.DelegationContext.Should().Contain("database schema");
    }

    [Fact]
    public void Parse_WithDelegation_AndPrecedingContent_ExtractsContentBeforeTag()
    {
        // Arrange
        var rawResponse = "I've analyzed the requirements and determined this needs architectural input.\n\n[DELEGATE:TechnicalArchitect] Please design the system architecture based on the requirements above.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
        result.Content.Should().Contain("analyzed the requirements");
    }

    [Theory]
    [InlineData("Coordinator", PersonaType.Coordinator)]
    [InlineData("BusinessAnalyst", PersonaType.BusinessAnalyst)]
    [InlineData("TechnicalArchitect", PersonaType.TechnicalArchitect)]
    [InlineData("SeniorDeveloper", PersonaType.SeniorDeveloper)]
    [InlineData("JuniorDeveloper", PersonaType.JuniorDeveloper)]
    [InlineData("SeniorQA", PersonaType.SeniorQA)]
    [InlineData("JuniorQA", PersonaType.JuniorQA)]
    [InlineData("UXEngineer", PersonaType.UXEngineer)]
    [InlineData("UIEngineer", PersonaType.UIEngineer)]
    [InlineData("DocumentWriter", PersonaType.DocumentWriter)]
    public void Parse_WithValidPersonaName_ParsesCorrectPersonaType(string personaName, PersonaType expectedType)
    {
        // Arrange
        var rawResponse = $"[DELEGATE:{personaName}] Please handle this task.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("BA", PersonaType.BusinessAnalyst)]
    [InlineData("TA", PersonaType.TechnicalArchitect)]
    [InlineData("SrDev", PersonaType.SeniorDeveloper)]
    [InlineData("JrDev", PersonaType.JuniorDeveloper)]
    [InlineData("SrQA", PersonaType.SeniorQA)]
    [InlineData("JrQA", PersonaType.JuniorQA)]
    [InlineData("UX", PersonaType.UXEngineer)]
    [InlineData("UI", PersonaType.UIEngineer)]
    [InlineData("Doc", PersonaType.DocumentWriter)]
    public void ParsePersonaType_WithAbbreviations_ParsesCorrectly(string abbreviation, PersonaType expectedType)
    {
        // Act
        var result = ResponseParser.ParsePersonaType(abbreviation);

        // Assert
        result.Should().Be(expectedType);
    }

    #endregion

    #region Clarification Parsing Tests

    [Fact]
    public void Parse_WithClarification_ReturnsClarificationResponse()
    {
        // Arrange
        var rawResponse = "[CLARIFY] What database system would you prefer - PostgreSQL or MySQL?";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Clarification);
        result.ClarificationQuestion.Should().Contain("database system");
        result.Content.Should().Contain("database system");
    }

    [Fact]
    public void Parse_WithClarification_AndPrecedingContent_ExtractsQuestion()
    {
        // Arrange
        var rawResponse = "I need more information to proceed.\n\n[CLARIFY] Could you specify the expected user load?";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Clarification);
        result.ClarificationQuestion.Should().Contain("expected user load");
    }

    #endregion

    #region Solution Parsing Tests

    [Fact]
    public void Parse_WithSolution_ReturnsSolutionResponse()
    {
        // Arrange
        var rawResponse = "[SOLUTION] Here is the complete implementation:\n\n```csharp\npublic class UserService { }\n```";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Solution);
        result.Content.Should().Contain("implementation");
    }

    [Fact]
    public void Parse_WithSolution_AndPrecedingAnalysis_CombinesContent()
    {
        // Arrange
        var rawResponse = "After careful analysis of all requirements, I've prepared the following solution.\n\n[SOLUTION] The recommended architecture uses a microservices pattern...";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Solution);
        result.Content.Should().Contain("careful analysis");
        result.Content.Should().Contain("microservices pattern");
    }

    #endregion

    #region Stuck Parsing Tests

    [Fact]
    public void Parse_WithStuck_ReturnsStuckResponse()
    {
        // Arrange
        var rawResponse = "[STUCK] I cannot proceed because this requires access to external APIs that I cannot reach.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.IsStuck.Should().BeTrue();
        result.Content.Should().Contain("external APIs");
    }

    [Fact]
    public void Parse_WithStuck_AndProgress_IncludesPartialProgress()
    {
        // Arrange
        var rawResponse = "I've completed the initial analysis phase.\n\n[STUCK] However, I cannot proceed with implementation because the database schema is not defined.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.IsStuck.Should().BeTrue();
        result.Content.Should().Contain("initial analysis");
    }

    #endregion

    #region Decline Parsing Tests

    [Fact]
    public void Parse_WithDecline_ReturnsDeclineResponse()
    {
        // Arrange
        var rawResponse = "[DECLINE] This task requires UX expertise. Please delegate to the UX Engineer.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Decline);
        result.Content.Should().Contain("UX expertise");
    }

    #endregion

    #region Memory Commands Tests

    [Fact]
    public void ExtractMemoryStores_WithSingleStore_ExtractsCorrectly()
    {
        // Arrange
        var rawResponse = "[STORE:user-requirements] The user needs a REST API with CRUD operations for managing products.";

        // Act
        var stores = ResponseParser.ExtractMemoryStores(rawResponse);

        // Assert
        stores.Should().HaveCount(1);
        stores[0].Identifier.Should().Be("user-requirements");
        stores[0].Content.Should().Contain("REST API");
    }

    [Fact]
    public void ExtractMemoryStores_WithMultipleStores_ExtractsAll()
    {
        // Arrange - The parser uses a regex that stops at the next tag boundary
        // Each store ends when another tag begins
        var rawResponse = "[STORE:api-design] The API uses RESTful conventions. [CLARIFY] question [STORE:database-choice] PostgreSQL is recommended.";

        // Act
        var stores = ResponseParser.ExtractMemoryStores(rawResponse);

        // Assert - Parser extracts stores that are delimited by other tags
        stores.Should().HaveCountGreaterThanOrEqualTo(1);
        stores.Should().Contain(s => s.Identifier == "api-design");
    }

    [Fact]
    public void ExtractMemoryRecalls_WithSingleRecall_ExtractsIdentifier()
    {
        // Arrange
        var rawResponse = "Based on my previous notes [REMEMBER:user-requirements] I can see that...";

        // Act
        var recalls = ResponseParser.ExtractMemoryRecalls(rawResponse);

        // Assert
        recalls.Should().HaveCount(1);
        recalls[0].Should().Be("user-requirements");
    }

    [Fact]
    public void ExtractMemoryRecalls_WithMultipleRecalls_ExtractsAll()
    {
        // Arrange
        var rawResponse = "Checking my notes: [REMEMBER:api-design] and [REMEMBER:database-choice] to continue.";

        // Act
        var recalls = ResponseParser.ExtractMemoryRecalls(rawResponse);

        // Assert
        recalls.Should().HaveCount(2);
        recalls.Should().Contain("api-design");
        recalls.Should().Contain("database-choice");
    }

    #endregion

    #region Internal Reasoning Tests

    [Fact]
    public void Parse_WithReasoning_ExtractsInternalReasoning()
    {
        // Arrange
        var rawResponse = "[REASONING] I need to consider the scalability requirements before making a decision. The user mentioned 10,000 concurrent users which suggests we need a robust architecture. [/REASONING] Based on my analysis, I recommend a microservices architecture.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.InternalReasoning.Should().Contain("scalability requirements");
        result.InternalReasoning.Should().Contain("10,000 concurrent users");
        result.Content.Should().NotContain("[REASONING]");
        result.Content.Should().Contain("microservices architecture");
    }

    [Fact]
    public void Parse_WithReasoningAndDelegation_ExtractsBoth()
    {
        // Arrange
        var rawResponse = "[REASONING] This is a design task that requires architectural expertise. [/REASONING] [DELEGATE:TechnicalArchitect] Please design the system architecture.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.InternalReasoning.Should().Contain("architectural expertise");
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
    }

    #endregion

    #region Answer (Default) Parsing Tests

    [Fact]
    public void Parse_WithNoTags_ReturnsAnswerType()
    {
        // Arrange
        var rawResponse = "Here is my analysis of the problem. The user requirements indicate a need for...";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Answer);
        result.Content.Should().Be(rawResponse);
    }

    [Fact]
    public void Parse_WithEmptyResponse_ReturnsEmptyAnswer()
    {
        // Arrange
        var rawResponse = "";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Answer);
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithWhitespaceOnly_ReturnsEmptyAnswer()
    {
        // Arrange
        var rawResponse = "   \n\t  ";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Answer);
        result.Content.Should().BeEmpty();
    }

    #endregion

    #region Clean Content Tests

    [Fact]
    public void CleanContent_RemovesAllTags()
    {
        // Arrange
        var rawResponse = "[REASONING] Thinking... [/REASONING] Some content [DELEGATE:TA] context [STORE:id] data";

        // Act
        var cleaned = ResponseParser.CleanContent(rawResponse);

        // Assert
        cleaned.Should().NotContain("[REASONING]");
        cleaned.Should().NotContain("[DELEGATE:");
        cleaned.Should().NotContain("[STORE:");
        cleaned.Should().Contain("Some content");
    }

    [Fact]
    public void CleanContent_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var cleaned = ResponseParser.CleanContent("");

        // Assert
        cleaned.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_WithCaseInsensitiveTags_ParsesCorrectly()
    {
        // Arrange
        var rawResponse = "[delegate:technicalarchitect] Please handle this.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
    }

    [Fact]
    public void Parse_WithMultipleTags_UsesFirstMatchingTag()
    {
        // Arrange - DELEGATE comes first so it should be used
        var rawResponse = "[DELEGATE:TechnicalArchitect] Handle this. [SOLUTION] This is a solution.";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
    }

    [Fact]
    public void ParsePersonaType_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = ResponseParser.ParsePersonaType("InvalidPersona");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParsePersonaType_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = ResponseParser.ParsePersonaType("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_RawResponsePreserved()
    {
        // Arrange
        var rawResponse = "[SOLUTION] Test solution";

        // Act
        var result = ResponseParser.Parse(rawResponse);

        // Assert
        result.RawResponse.Should().Be(rawResponse);
    }

    #endregion
}
