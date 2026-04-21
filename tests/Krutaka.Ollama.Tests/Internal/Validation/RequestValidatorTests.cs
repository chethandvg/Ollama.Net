using FluentAssertions;
using Krutaka.Ollama.Internal.Validation;
using Xunit;

namespace Krutaka.Ollama.Tests.Internal.Validation;

public sealed class RequestValidatorTests
{
    private static readonly string[] SingleMessage = ["x"];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ValidateModel_InvalidInput_ShouldThrowArgumentException(string? model)
    {
        Action act = () => RequestValidator.ValidateModel(model);
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(model));
    }

    [Theory]
    [InlineData("llama3")]
    [InlineData("qwen2:7b")]
    [InlineData("user/custom-model:latest")]
    public void ValidateModel_ValidInput_ShouldNotThrow(string model)
    {
        Action act = () => RequestValidator.ValidateModel(model);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateMessages_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => RequestValidator.ValidateMessages<string>(null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("messages");
    }

    [Fact]
    public void ValidateMessages_Empty_ShouldThrowArgumentException()
    {
        Action act = () => RequestValidator.ValidateMessages(Array.Empty<string>());
        act.Should().Throw<ArgumentException>()
            .WithMessage("*empty*")
            .WithParameterName("messages");
    }

    [Fact]
    public void ValidateMessages_NonEmpty_ShouldNotThrow()
    {
        Action act = () => RequestValidator.ValidateMessages(SingleMessage);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ValidateStreamMode_MatchingOrNull_ShouldNotThrow(bool? requestStream, bool expected)
    {
        Action act = () => RequestValidator.ValidateStreamMode(requestStream, expected, "Chat");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStreamMode_MismatchExpectingStream_ShouldSuggestStreamingMethod()
    {
        Action act = () => RequestValidator.ValidateStreamMode(requestStream: false, expectedStream: true, "Chat");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ChatStreamAsync*");
    }

    [Fact]
    public void ValidateStreamMode_WithAsyncSuffixedName_ShouldStripSuffixInSuggestion()
    {
        // Regression: nameof(ChatAsync) used to produce suggestion "ChatAsyncStreamAsync".
        Action act = () => RequestValidator.ValidateStreamMode(requestStream: true, expectedStream: false, methodName: "ChatAsync");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ChatAsync*non-streaming*")
            .Where(e => !e.Message.Contains("ChatAsyncStreamAsync", StringComparison.Ordinal));

        Action act2 = () => RequestValidator.ValidateStreamMode(requestStream: false, expectedStream: true, methodName: "ChatAsync");
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("*ChatStreamAsync*")
            .Where(e => !e.Message.Contains("ChatAsyncStreamAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateStreamMode_WithoutAsyncSuffix_ShouldStillSuggestCorrectly()
    {
        Action act = () => RequestValidator.ValidateStreamMode(requestStream: false, expectedStream: true, methodName: "Generate");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GenerateStreamAsync*");
    }
}
