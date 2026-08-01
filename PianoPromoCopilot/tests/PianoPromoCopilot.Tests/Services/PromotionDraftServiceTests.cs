using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Services;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Domain.Enums;
using PianoPromoCopilot.Infrastructure.Data;
using Xunit;

namespace PianoPromoCopilot.Tests.Services;

public class PromotionDraftServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<AppDbContext> CreateDbWithVideoAsync(string videoId)
    {
        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new YouTubeVideo
        {
            YouTubeVideoId = videoId,
            Title = "Moonlit Reverie",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return db;
    }

    private static PromotionDraftService CreateService(
        IAppDbContext db,
        OptimizeVideoResponse optimizeResult)
    {
        var optimizer = new Mock<IVideoOptimizationService>();
        optimizer
            .Setup(o => o.OptimizeAsync(It.IsAny<OptimizeVideoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizeResult);

        return new PromotionDraftService(db, optimizer.Object, NullLogger<PromotionDraftService>.Instance);
    }

    private static OptimizeVideoResponse SafeOptimizeResult() => new()
    {
        SocialPosts = new SocialPostsDto
        {
            Instagram = "Instagram post",
            TikTok = "TikTok post",
            Facebook = "",          // empty platforms must not become drafts
            Reddit = "   ",         // whitespace-only must not become drafts either
            X = "X post",
            LinkedIn = "LinkedIn post",
            EmailNewsletter = "Email post"
        },
        Compliance = new ComplianceSummaryDto
        {
            RiskLevel = RiskLevel.Low,
            Issues = Array.Empty<string>(),
            IsSafeToUse = true
        }
    };

    [Fact]
    public async Task GenerateDraftsAsync_UnknownVideo_ReturnsVideoNotFoundAndWritesNothing()
    {
        var db = CreateInMemoryDbContext();
        var sut = CreateService(db, SafeOptimizeResult());

        var result = await sut.GenerateDraftsAsync("does_not_exist");

        result.Outcome.Should().Be(GeneratePromotionDraftsOutcome.VideoNotFound);
        result.Drafts.Should().BeEmpty();
        db.PromotionDrafts.Should().BeEmpty();
        db.ComplianceReviews.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateDraftsAsync_SafeContent_PersistsOneDraftPerNonEmptyPlatform()
    {
        var db = await CreateDbWithVideoAsync("draft_ok_001");
        var sut = CreateService(db, SafeOptimizeResult());

        var result = await sut.GenerateDraftsAsync("draft_ok_001");

        result.Outcome.Should().Be(GeneratePromotionDraftsOutcome.Created);
        result.Drafts.Should().HaveCount(5);
        result.Drafts.Select(d => d.Platform)
            .Should().BeEquivalentTo(new[] { "Instagram", "TikTok", "X", "LinkedIn", "Email" });
        result.Drafts.Should().OnlyContain(d => d.Status == PromotionStatus.Draft);

        // Nothing is posted - every draft awaits a human.
        db.PromotionDrafts.Should().OnlyContain(d => d.PostedAt == null && d.Status == PromotionStatus.Draft);
    }

    [Fact]
    public async Task GenerateDraftsAsync_BlockedContent_SavesNoDraftsAndAuditsTheRefusal()
    {
        var db = await CreateDbWithVideoAsync("draft_blocked_001");

        var blocked = SafeOptimizeResult();
        blocked.Compliance = new ComplianceSummaryDto
        {
            RiskLevel = RiskLevel.Blocked,
            Issues = new[] { "Suggests purchasing views which violates YouTube ToS" },
            IsSafeToUse = false
        };

        var sut = CreateService(db, blocked);

        var result = await sut.GenerateDraftsAsync("draft_blocked_001");

        result.Outcome.Should().Be(GeneratePromotionDraftsOutcome.ComplianceBlocked);
        result.Compliance.RiskLevel.Should().Be(RiskLevel.Blocked);
        result.Drafts.Should().BeEmpty();

        // Compliance gates persistence.
        db.PromotionDrafts.Should().BeEmpty();

        // ...and the refusal is auditable under its own source type.
        var audit = db.ComplianceReviews.Single();
        audit.SourceType.Should().Be(ComplianceSourceType.PromotionDraft);
        audit.RiskLevel.Should().Be(RiskLevel.Blocked);
        audit.Approved.Should().BeFalse();
        audit.Issues.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateDraftsAsync_Regenerating_SupersedesUnreviewedButKeepsHumanDecisions()
    {
        var db = await CreateDbWithVideoAsync("draft_regen_001");
        var sut = CreateService(db, SafeOptimizeResult());

        var firstBatch = await sut.GenerateDraftsAsync("draft_regen_001");
        var batchSize = firstBatch.Drafts.Count;

        // A human acts on one draft...
        var approved = db.PromotionDrafts.First(d => d.YouTubeVideoId == "draft_regen_001");
        approved.Status = PromotionStatus.Approved;
        await db.SaveChangesAsync();

        await sut.GenerateDraftsAsync("draft_regen_001");

        // ...their decision survives regeneration...
        db.PromotionDrafts.Single(d => d.Id == approved.Id).Status.Should().Be(PromotionStatus.Approved);

        // ...and the un-reviewed batch was replaced, not appended to: exactly one fresh batch of
        // Draft rows, plus the single row the human already acted on.
        db.PromotionDrafts.Count(d => d.Status == PromotionStatus.Draft).Should().Be(batchSize);
        db.PromotionDrafts.Count(d => d.YouTubeVideoId == "draft_regen_001").Should().Be(batchSize + 1);
    }

    [Fact]
    public async Task GenerateDraftsAsync_SafeContent_AuditsTheApproval()
    {
        var db = await CreateDbWithVideoAsync("draft_audit_001");
        var sut = CreateService(db, SafeOptimizeResult());

        await sut.GenerateDraftsAsync("draft_audit_001");

        var audit = db.ComplianceReviews.Single();
        audit.SourceType.Should().Be(ComplianceSourceType.PromotionDraft);
        audit.Approved.Should().BeTrue();
        audit.RiskLevel.Should().Be(RiskLevel.Low);
    }
}
