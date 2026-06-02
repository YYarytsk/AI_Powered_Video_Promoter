using FluentAssertions;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Services;
using Xunit;

namespace PianoPromoCopilot.Tests.Services;

public class AnalyticsRecommendationServiceTests
{
    private readonly AnalyticsRecommendationService _sut = new();

    private static AnalyticsSnapshotDto CreateSnapshot(
        string videoId = "test_video",
        long? views = null,
        long? likes = null,
        long? comments = null,
        int? avgViewDurationSeconds = null,
        decimal? avgViewPercentage = null,
        long? impressions = null,
        decimal? ctr = null,
        string? trafficSource = null,
        string? country = null)
    {
        return new AnalyticsSnapshotDto
        {
            YouTubeVideoId = videoId,
            SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Views = views,
            Likes = likes,
            Comments = comments,
            AverageViewDurationSeconds = avgViewDurationSeconds,
            AverageViewPercentage = avgViewPercentage,
            Impressions = impressions,
            ImpressionClickThroughRate = ctr,
            TrafficSource = trafficSource,
            Country = country,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void GenerateRecommendations_EmptySnapshots_ReturnsInitialPromotion()
    {
        // Act
        var result = _sut.GenerateRecommendations(Array.Empty<AnalyticsSnapshotDto>());

        // Assert
        result.Should().HaveCount(1);
        result[0].RecommendationType.Should().Be("InitialPromotion");
        result[0].Priority.Should().Be(1);
    }

    [Fact]
    public void GenerateRecommendations_HighRetentionLowCtr_RecommendsTitleThumbnail()
    {
        // Arrange - high avg view duration but very low CTR
        var snapshots = new[]
        {
            CreateSnapshot(avgViewDurationSeconds: 200, ctr: 0.015m)
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "TitleThumbnailImprovement");
    }

    [Fact]
    public void GenerateRecommendations_HighCtrLowRetention_RecommendsIntroImprovement()
    {
        // Arrange - high CTR but viewers leaving early
        var snapshots = new[]
        {
            CreateSnapshot(ctr: 0.08m, avgViewPercentage: 0.20m)
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "IntroImprovement");
    }

    [Fact]
    public void GenerateRecommendations_SearchTraffic_RecommendsSeO()
    {
        // Arrange
        var snapshots = new[]
        {
            CreateSnapshot(trafficSource: "YT_SEARCH")
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "SeoOptimization");
    }

    [Fact]
    public void GenerateRecommendations_ExternalTraffic_RecommendsSocialAmplification()
    {
        // Arrange
        var snapshots = new[]
        {
            CreateSnapshot(trafficSource: "EXT_URL")
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "SocialAmplification");
    }

    [Fact]
    public void GenerateRecommendations_NonUsCountry_RecommendsLocalization()
    {
        // Arrange
        var snapshots = new[]
        {
            CreateSnapshot(country: "JP")
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "Localization");
    }

    [Fact]
    public void GenerateRecommendations_ViewsIncreasingLowEngagement_RecommendsCallToAction()
    {
        // Arrange - views increasing but very low like ratio
        var snapshots = new[]
        {
            CreateSnapshot(views: 100, likes: 1),
            CreateSnapshot(views: 200, likes: 2) // low like/view ratio
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().Contain(r => r.RecommendationType == "CallToAction");
    }

    [Fact]
    public void GenerateRecommendations_AlwaysReturnsSomething()
    {
        // Arrange - healthy metrics that trigger no specific rules
        var snapshots = new[]
        {
            CreateSnapshot(views: 500, likes: 50, country: "US", trafficSource: "YT_CHANNEL")
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateRecommendations_AreOrderedByPriority()
    {
        // Arrange
        var snapshots = new[]
        {
            CreateSnapshot(trafficSource: "YT_SEARCH", ctr: 0.08m, avgViewPercentage: 0.20m)
        };

        // Act
        var result = _sut.GenerateRecommendations(snapshots);

        // Assert - should be ordered by priority ascending
        var priorities = result.Select(r => r.Priority).ToList();
        priorities.Should().BeInAscendingOrder();
    }
}
