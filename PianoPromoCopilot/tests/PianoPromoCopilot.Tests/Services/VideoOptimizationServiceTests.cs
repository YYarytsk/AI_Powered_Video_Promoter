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

    [Fact]
    public async Task OptimizeAsync_RecordsComplianceReviewAuditEntry()
    {
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["A peaceful piano piece"],
                  "descriptions": ["An original composition"],
                  "tags": ["piano"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": ["Hands on keys"],
                  "shortsIdeas": [],
                  "socialPosts": { "instagram": "New piece out now" }
                }
                """);

        var db = CreateInMemoryDbContext();
        var sut = CreateService(llmMock.Object, db);

        await sut.OptimizeAsync(new OptimizeVideoRequest { Title = "Test Piano Piece" });

        var audit = db.ComplianceReviews.Single();
        audit.SourceType.Should().Be("LlmSuggestion");
        audit.RiskLevel.Should().Be("Low");
        audit.Approved.Should().BeTrue();
        audit.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task OptimizeAsync_BlockedContent_IsAuditedAndNotSaved()
    {
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Buy views for your piano channel"],
                  "descriptions": ["D"],
                  "tags": ["piano"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": [],
                  "shortsIdeas": [],
                  "socialPosts": {}
                }
                """);

        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new PianoPromoCopilot.Domain.Entities.YouTubeVideo
        {
            YouTubeVideoId = "test_blocked_001",
            Title = "Test Video",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(llmMock.Object, db);

        var result = await sut.OptimizeAsync(new OptimizeVideoRequest
        {
            YouTubeVideoId = "test_blocked_001",
            Title = "Test Piano Piece"
        });

        // The verdict reaches the caller...
        result.Compliance.RiskLevel.Should().Be("Blocked");
        result.Compliance.IsSafeToUse.Should().BeFalse();

        // ...the refusal is auditable...
        var audit = db.ComplianceReviews.Single();
        audit.RiskLevel.Should().Be("Blocked");
        audit.Approved.Should().BeFalse();
        audit.Issues.Should().NotBeNullOrEmpty();

        // ...and nothing violating was persisted for a human to approve.
        db.VideoOptimizationSuggestions
            .Count(s => s.YouTubeVideoId == "test_blocked_001")
            .Should().Be(0);
    }

    [Fact]
    public async Task OptimizeAsync_CalledTwice_DoesNotDuplicateSuggestions()
    {
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Title A", "Title B"],
                  "descriptions": ["Desc"],
                  "tags": ["piano"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": ["Idea"],
                  "shortsIdeas": [],
                  "socialPosts": { "instagram": "Post" }
                }
                """);

        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new PianoPromoCopilot.Domain.Entities.YouTubeVideo
        {
            YouTubeVideoId = "test_dedupe_001",
            Title = "Test Video",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(llmMock.Object, db);
        var request = new OptimizeVideoRequest
        {
            YouTubeVideoId = "test_dedupe_001",
            Title = "Test Piano Piece"
        };

        await sut.OptimizeAsync(request);
        var afterFirst = db.VideoOptimizationSuggestions.Count(s => s.YouTubeVideoId == "test_dedupe_001");

        await sut.OptimizeAsync(request);
        var afterSecond = db.VideoOptimizationSuggestions.Count(s => s.YouTubeVideoId == "test_dedupe_001");

        afterFirst.Should().BeGreaterThan(0);
        afterSecond.Should().Be(afterFirst, "re-optimizing must not duplicate identical suggestions");
    }

    [Fact]
    public async Task OptimizeAsync_ReOptimize_PreservesApprovalDecisions()
    {
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["Title A"],
                  "descriptions": ["Desc"],
                  "tags": ["piano"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": [],
                  "shortsIdeas": [],
                  "socialPosts": {}
                }
                """);

        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new PianoPromoCopilot.Domain.Entities.YouTubeVideo
        {
            YouTubeVideoId = "test_preserve_001",
            Title = "Test Video",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(llmMock.Object, db);
        var request = new OptimizeVideoRequest
        {
            YouTubeVideoId = "test_preserve_001",
            Title = "Test Piano Piece"
        };

        await sut.OptimizeAsync(request);

        // A human approves one suggestion...
        var approved = db.VideoOptimizationSuggestions.First(s => s.YouTubeVideoId == "test_preserve_001");
        approved.IsApproved = true;
        await db.SaveChangesAsync();

        // ...and re-optimizing must not silently discard that decision.
        await sut.OptimizeAsync(request);

        db.VideoOptimizationSuggestions
            .Single(s => s.Id == approved.Id)
            .IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task OptimizeAsync_StoresShortsIdeaAsCamelCaseJson()
    {
        // The saved ShortsIdea payload is JSON that clients parse directly, so its
        // casing must match the camelCase the rest of the API returns.
        var llmMock = new Mock<ILlmService>();
        llmMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "titles": ["T"],
                  "descriptions": ["D"],
                  "tags": ["piano"],
                  "hashtags": ["#piano"],
                  "thumbnailIdeas": ["Idea"],
                  "shortsIdeas": [
                    {
                      "title": "Shorts Title",
                      "hook": "Watch this!",
                      "suggestedTimestamp": "1:30",
                      "description": "Shorts description",
                      "caption": "Caption"
                    }
                  ],
                  "socialPosts": { "instagram": "Post" }
                }
                """);

        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new PianoPromoCopilot.Domain.Entities.YouTubeVideo
        {
            YouTubeVideoId = "test_shorts_001",
            Title = "Test Video",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(llmMock.Object, db);

        // Act
        await sut.OptimizeAsync(new OptimizeVideoRequest
        {
            YouTubeVideoId = "test_shorts_001",
            Title = "Test Piano Piece"
        });

        // Assert
        var shorts = db.VideoOptimizationSuggestions
            .Single(s => s.YouTubeVideoId == "test_shorts_001" && s.SuggestionType == "ShortsIdea");

        shorts.SuggestionText.Should().Contain("\"title\":");
        shorts.SuggestionText.Should().Contain("\"suggestedTimestamp\":");
        shorts.SuggestionText.Should().NotContain("\"Title\":");
        shorts.SuggestionText.Should().NotContain("\"SuggestedTimestamp\":");

        // And it must round-trip into the same DTO the API exposes, under the same
        // camelCase policy the API uses — no case-insensitive fallback needed.
        var parsed = System.Text.Json.JsonSerializer.Deserialize<ShortsIdeaDto>(
            shorts.SuggestionText,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = false
            });

        parsed.Should().NotBeNull();
        parsed!.Title.Should().Be("Shorts Title");
        parsed.SuggestedTimestamp.Should().Be("1:30");
    }
}
