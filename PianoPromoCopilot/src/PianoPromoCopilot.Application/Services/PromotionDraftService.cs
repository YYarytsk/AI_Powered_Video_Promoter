using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Mapping;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Application.Services;

/// <summary>
/// Orchestrates promotion draft generation. This used to live in PromotionDraftsController, where
/// the compliance gate, the supersede rule and the platform list all sat next to HTTP concerns and
/// could not be tested without spinning up the API.
///
/// Composition note: <see cref="IVideoOptimizationService.OptimizeAsync"/> has a side effect - it
/// persists suggestions - so calling it here also refreshes the video's suggestions. That is the
/// pre-existing behaviour and the MVP flow depends on it.
/// </summary>
public class PromotionDraftService : IPromotionDraftService
{
    private readonly IAppDbContext _dbContext;
    private readonly IVideoOptimizationService _optimizationService;
    private readonly ILogger<PromotionDraftService> _logger;

    public PromotionDraftService(
        IAppDbContext dbContext,
        IVideoOptimizationService optimizationService,
        ILogger<PromotionDraftService> logger)
    {
        _dbContext = dbContext;
        _optimizationService = optimizationService;
        _logger = logger;
    }

    public async Task<GeneratePromotionDraftsResult> GenerateDraftsAsync(
        string youTubeVideoId,
        CancellationToken cancellationToken = default)
    {
        var video = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == youTubeVideoId, cancellationToken);

        if (video == null)
        {
            return GeneratePromotionDraftsResult.VideoNotFound();
        }

        var optimized = await _optimizationService.OptimizeAsync(
            new OptimizeVideoRequest
            {
                YouTubeVideoId = youTubeVideoId,
                Title = video.Title,
                Description = video.Description,
                CompositionName = video.CompositionName,
                Mood = video.Mood,
                Style = video.Style,
                TargetAudience = video.TargetAudience
            },
            cancellationToken);

        var compliance = optimized.Compliance;

        // Drafts are a distinct decision from the suggestions the optimizer already audited, so
        // they get their own audit row under their own source type. Without this the only record
        // of a refused draft batch was a log line.
        await RecordComplianceReviewAsync(video.Id, compliance, cancellationToken);

        // Compliance gates persistence - a Blocked verdict never reaches the database.
        if (compliance.RiskLevel == RiskLevel.Blocked)
        {
            _logger.LogWarning(
                "Compliance review blocked promotion draft generation for video {VideoId}. Risk {RiskLevel}, issues: {Issues}",
                youTubeVideoId, compliance.RiskLevel, string.Join("; ", compliance.Issues));

            return GeneratePromotionDraftsResult.Blocked(compliance);
        }

        var now = DateTime.UtcNow;

        var drafts = SocialPostPlatforms.EnumerateNonEmpty(optimized.SocialPosts)
            .Select(p => new PromotionDraft
            {
                YouTubeVideoId = youTubeVideoId,
                Platform = p.Platform,
                DraftText = p.Text,
                Status = PromotionStatus.Draft,
                CreatedAt = now
            })
            .ToList();

        // Supersede the previous generation, but never discard a draft the user already acted on.
        var supersededDrafts = await _dbContext.PromotionDrafts
            .Where(d => d.YouTubeVideoId == youTubeVideoId && d.Status == PromotionStatus.Draft)
            .ToListAsync(cancellationToken);

        if (supersededDrafts.Count > 0)
        {
            _dbContext.PromotionDrafts.RemoveRange(supersededDrafts);
            _logger.LogInformation(
                "Superseded {Count} untouched promotion drafts for video {VideoId}",
                supersededDrafts.Count, youTubeVideoId);
        }

        await _dbContext.PromotionDrafts.AddRangeAsync(drafts, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated {Count} promotion drafts for video {VideoId} (compliance risk {RiskLevel})",
            drafts.Count, youTubeVideoId, compliance.RiskLevel);

        return GeneratePromotionDraftsResult.Created(
            compliance,
            drafts.Select(d => d.ToDto()).ToList());
    }

    /// <summary>
    /// Auditing must never break the request it is recording, so failures are logged and swallowed.
    /// </summary>
    private async Task RecordComplianceReviewAsync(
        int videoId,
        ComplianceSummaryDto compliance,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.ComplianceReviews.AddAsync(new ComplianceReview
            {
                SourceType = ComplianceSourceType.PromotionDraft,
                SourceId = videoId,
                RiskLevel = compliance.RiskLevel,
                Issues = compliance.Issues.Length > 0 ? string.Join("; ", compliance.Issues) : null,
                Approved = compliance.RiskLevel != RiskLevel.Blocked,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to record promotion draft compliance review audit entry");
        }
    }
}
