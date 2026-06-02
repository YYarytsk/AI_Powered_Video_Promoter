using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Services;
using PianoPromoCopilot.Infrastructure.Data;
using Xunit;

namespace PianoPromoCopilot.Tests.Services;

public class VideoOptimizationServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static VideoOptimizationService CreateService(
        ILlmService? llmService = null,
        IAppDbContext? dbContext = null)
    {
        var mockLlm = llmService ?? new Mock<ILlmService>().Object;
        var db = dbContext ?? CreateInMemoryDbContext();
        var compliance = new ComplianceReviewService();
        var logger = NullLogger<VideoOptimizationService>.Instance;

        return new VideoOptimizationService(mockLlm, compliance, db, logger);
    }

    [Fact]
    public async Task OptimizeAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Test Title 1", "Test Title 2"],
                  "descriptions": ["Test description"],
                  "tags": ["piano", "music"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": ["Close-up of piano keys"],
                  "shortsIdeas": [],
                  "socialPosts": {
                    "instagram": "Instagram post",
                    "tiktok": "TikTok post",
                    "facebook": "Facebook post",
                    "reddit": "Reddit post",
                    "x": "X post",
                    "linkedin": "LinkedIn post",
                    "emailNewsletter": "Email newsletter"
                  }
                }
                """);

        var sut = CreateService(llmMock.Object);
        var request = new OptimizeVideoRequest { Title = "Test Piano Piece" };

        // Act
        var result = await sut.OptimizeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Titles.Should().HaveCount(2);
        result.Descriptions.Should().HaveCount(1);
        result.Tags.Should().Contain("piano");
        result.Hashtags.Should().Contain("#piano");
        result.SocialPosts.Instagram.Should().Be("Instagram post");
        result.Compliance.Should().NotBeNull();
        result.Compliance.RiskLevel.Should().Be("Low");
    }

    [Fact]
    public async Task OptimizeAsync_LlmThrows_UsesFallback()
    {
        // Arrange
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateService(llmMock.Object);
        var request = new OptimizeVideoRequest { Title = "Moonlit Reverie", Mood = "peaceful" };

        // Act
        var result = await sut.OptimizeAsync(request);

        // Assert - should fall back to mock data
        result.Should().NotBeNull();
        result.Titles.Should().NotBeEmpty();
        result.Descriptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_InvalidJson_UsesFallback()
    {
        // Arrange
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is not valid JSON at all!");

        var sut = CreateService(llmMock.Object);
        var request = new OptimizeVideoRequest { Title = "Test Piano" };

        // Act
        var result = await sut.OptimizeAsync(request);

        // Assert - falls back gracefully
        result.Should().NotBeNull();
        result.Titles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_ComplianceChecksAllOutput()
    {
        // Arrange
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Clean title"],
                  "descriptions": ["Clean description - you should buy views to grow faster"],
                  "tags": ["piano"],
                  "hashtags": [],
                  "thumbnailIdeas": [],
                  "shortsIdeas": [],
                  "socialPosts": {
                    "instagram": "",
                    "tiktok": "",
                    "facebook": "",
                    "reddit": "",
                    "x": "",
                    "linkedin": "",
                    "emailNewsletter": ""
                  }
                }
                """);

        var sut = CreateService(llmMock.Object);
        var request = new OptimizeVideoRequest { Title = "Test Piano" };

        // Act
        var result = await sut.OptimizeAsync(request);

        // Assert - compliance should catch the "buy views" in description
        result.Compliance.RiskLevel.Should().Be("Blocked");
        result.Compliance.IsSafeToUse.Should().BeFalse();
        result.Compliance.Issues.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_WithVideoId_SavesSuggestionsToDb()
    {
        // Arrange
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Saved Title 1", "Saved Title 2"],
                  "descriptions": ["Saved description"],
                  "tags": ["piano", "music"],
                  "hashtags": ["#piano", "#music"],
                  "thumbnailIdeas": ["Piano close-up"],
                  "shortsIdeas": [
                    {
                      "title": "Shorts Title",
                      "hook": "Watch this!",
                      "suggestedTimestamp": "1:30",
                      "description": "Shorts description",
                      "caption": "Caption"
                    }
                  ],
                  "socialPosts": {
                    "instagram": "Instagram post",
                    "tiktok": "",
                    "facebook": "",
                    "reddit": "",
                    "x": "",
                    "linkedin": "",
                    "emailNewsletter": ""
                  }
                }
                """);

        var db = CreateInMemoryDbContext();

        // Add a video first
        db.YouTubeVideos.Add(new PianoPromoCopilot.Domain.Entities.YouTubeVideo
        {
            YouTubeVideoId = "test_save_001",
            Title = "Test Video",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(llmMock.Object, db);
        var request = new OptimizeVideoRequest
        {
            YouTubeVideoId = "test_save_001",
            Title = "Test Piano Piece"
        };

        // Act
        await sut.OptimizeAsync(request);

        // Assert - suggestions should be saved
        var savedSuggestions = db.VideoOptimizationSuggestions
            .Where(s => s.YouTubeVideoId == "test_save_001")
            .ToList();

        savedSuggestions.Should().NotBeEmpty();
        savedSuggestions.Should().Contain(s => s.SuggestionType == "Title");
        savedSuggestions.Should().Contain(s => s.SuggestionType == "Tags");
    }
}
