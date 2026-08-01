using FluentAssertions;
using PianoPromoCopilot.Application.Services;
using PianoPromoCopilot.Domain.Enums;
using Xunit;

namespace PianoPromoCopilot.Tests.Services;

public class ComplianceReviewServiceTests
{
    private readonly ComplianceReviewService _sut = new();

    [Fact]
    public void Review_CleanText_ReturnsLowRisk()
    {
        // Arrange
        var text = "Beautiful original piano composition - gentle, peaceful music for relaxation and study.";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.Issues.Should().BeEmpty();
        result.IsSafeToUse.Should().BeTrue();
    }

    [Fact]
    public void Review_BuyViews_ReturnsBlocked()
    {
        // Arrange
        var text = "You should buy views to boost your channel quickly!";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.IsSafeToUse.Should().BeFalse();
        result.Issues.Should().NotBeEmpty();
    }

    [Fact]
    public void Review_GuaranteedViral_ReturnsHighRisk()
    {
        // Arrange
        var text = "This strategy is guaranteed viral success for your channel!";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Fact]
    public void Review_SubForSub_ReturnsBlocked()
    {
        // Arrange
        var text = "Let's do sub for sub - subscribe to me and I'll subscribe back!";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Fact]
    public void Review_FakeSubscribers_ReturnsBlocked()
    {
        // Arrange
        var text = "Get 10,000 fake subscribers overnight!";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Fact]
    public void Review_WorldsBestPianist_ReturnsMediumRisk()
    {
        // Arrange
        var text = "Listen to the world's best pianist perform this stunning piece!";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Medium);
        result.Issues.Should().NotBeEmpty();
    }

    [Fact]
    public void ReviewAll_MultipleTextsOneBlocked_ReturnsBlocked()
    {
        // Arrange
        var texts = new[]
        {
            "Beautiful original piano composition",
            "Please sub for sub with me",
            "Original romantic piano music"
        };

        // Act
        var result = _sut.ReviewAll(texts);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Fact]
    public void ReviewAll_AllClean_ReturnsLowRisk()
    {
        // Arrange
        var texts = new[]
        {
            "Original piano composition - peaceful and relaxing",
            "Subscribe for more original piano music",
            "Piano piece composed in C major"
        };

        // Act
        var result = _sut.ReviewAll(texts);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.IsSafeToUse.Should().BeTrue();
    }

    [Fact]
    public void Review_EmptyText_ReturnsLowRisk()
    {
        // Arrange
        var text = "";

        // Act
        var result = _sut.Review(text);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.IsSafeToUse.Should().BeTrue();
    }

    [Fact]
    public void Review_SpamKeyword_ReturnsHighRisk()
    {
        // Arrange
        var text = "Mass spam this link to every YouTube channel comment section";

        // Act
        var result = _sut.Review(text);

        // Assert - "spam" alone is High risk; "mass DM" would be Blocked
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Fact]
    public void Review_FakeLikes_ReturnsBlocked()
    {
        var result = _sut.Review("Get fake likes on every upload to boost the algorithm");

        result.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.IsSafeToUse.Should().BeFalse();
        result.Issues.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Use engagement bots to grow faster")]   // plural
    [InlineData("bot")]                                  // whole text is the word
    [InlineData("Set up a bot")]                         // end of text
    [InlineData("Bots will inflate your numbers")]       // start of text, capitalised
    public void Review_BotAsWholeWord_ReturnsHighRisk(string text)
    {
        // The old rule was the substring " bot " and so could not match at a field edge or a plural.
        var result = _sut.Review(text);

        result.RiskLevel.Should().Be(RiskLevel.High);
        result.IsSafeToUse.Should().BeFalse();
    }

    [Theory]
    [InlineData("A robot arm turns the pages of the score")]
    [InlineData("Recorded from the bottom of the piano frame")]
    [InlineData("Both hands play the theme in octaves")]
    [InlineData("Nothing here would sabotage your channel")]
    public void Review_WordsContainingBot_AreNotFlagged(string text)
    {
        // Word-boundary matching must not fire on robot / bottom / both / sabotage.
        var result = _sut.Review(text);

        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.IsSafeToUse.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }
}
