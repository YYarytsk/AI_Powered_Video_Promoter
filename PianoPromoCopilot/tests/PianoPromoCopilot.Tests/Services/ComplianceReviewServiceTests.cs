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
}
