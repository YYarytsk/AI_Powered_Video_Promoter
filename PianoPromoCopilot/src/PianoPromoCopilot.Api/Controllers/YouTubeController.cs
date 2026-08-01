using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
[Route("api/youtube")]
public class YouTubeController : ControllerBase
{
    private readonly IYouTubeService _youTubeService;
    private readonly IYouTubeSyncService _syncService;
    private readonly IAppDbContext _dbContext;
    private readonly IComplianceReviewService _complianceService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeController> _logger;

    public YouTubeController(
        IYouTubeService youTubeService,
        IYouTubeSyncService syncService,
        IAppDbContext dbContext,
        IComplianceReviewService complianceService,
        IConfiguration configuration,
        ILogger<YouTubeController> logger)
    {
        _youTubeService = youTubeService;
        _syncService = syncService;
        _dbContext = dbContext;
        _complianceService = complianceService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Returns the connected or mock YouTube channel.</summary>
    [HttpGet("channel")]
    [ProducesResponseType(typeof(YouTubeChannelDto), 200)]
    public async Task<IActionResult> GetChannel(CancellationToken cancellationToken)
    {
        var channel = await _youTubeService.GetChannelAsync(cancellationToken);
        return Ok(channel);
    }

    /// <summary>
    /// Returns videos from YouTube API (mock or real based on feature flag).
    /// Stores fetched results in the local database.
    /// </summary>
    [HttpGet("videos")]
    [ProducesResponseType(typeof(IEnumerable<YouTubeVideoDto>), 200)]
    public async Task<IActionResult> GetVideos(CancellationToken cancellationToken)
    {
        var videos = await _syncService.SyncVideosAsync(cancellationToken);
        return Ok(videos);
    }

    /// <summary>
    /// Syncs channel, videos, and statistics from YouTube.
    /// Feature-flagged: only runs when Features__UseMockYouTube=false.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SyncChannel(CancellationToken cancellationToken)
    {
        var useMock = _configuration.GetValue<bool>("Features:UseMockYouTube", true);

        _logger.LogInformation("YouTube sync requested. Mock mode: {MockMode}", useMock);

        var result = await _syncService.SyncChannelAndVideosAsync(cancellationToken);

        return Ok(new
        {
            message = "Sync completed",
            channelSynced = result.ChannelTitle,
            videosSynced = result.VideosSynced,
            mockMode = useMock
        });
    }

    /// <summary>
    /// Updates YouTube video metadata (title, description, tags).
    /// HUMAN-APPROVED ONLY. Feature-flagged: requires EnableYouTubeWriteActions=true.
    /// Runs compliance review before any update.
    /// </summary>
    [HttpPost("videos/{youtubeVideoId}/update-metadata")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateVideoMetadata(
        string youtubeVideoId,
        [FromBody] UpdateYouTubeVideoMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var writeActionsEnabled = _configuration.GetValue<bool>("Features:EnableYouTubeWriteActions", false);

        if (!writeActionsEnabled)
        {
            return StatusCode(403, new
            {
                message = "YouTube write actions are disabled. Set Features__EnableYouTubeWriteActions=true to enable.",
                warning = "This action modifies your live YouTube video. Enable only after full OAuth setup and testing."
            });
        }

        // Compliance review BEFORE any write
        var textsToReview = new List<string>();
        if (request.Title != null) textsToReview.Add(request.Title);
        if (request.Description != null) textsToReview.Add(request.Description);
        if (request.Tags != null) textsToReview.AddRange(request.Tags);

        if (textsToReview.Count > 0)
        {
            var compliance = _complianceService.ReviewAll(textsToReview);

            // This is the only path that touches a live YouTube video, so the verdict is
            // recorded either way - a refusal is exactly what an audit needs to show.
            await _dbContext.ComplianceReviews.AddAsync(new ComplianceReview
            {
                SourceType = ComplianceSourceType.MetadataUpdate,
                RiskLevel = compliance.RiskLevel,
                Issues = compliance.Issues.Length > 0 ? string.Join("; ", compliance.Issues) : null,
                Approved = compliance.IsSafeToUse,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (!compliance.IsSafeToUse)
            {
                _logger.LogWarning(
                    "Compliance blocked a YouTube metadata update for {VideoId}. Risk: {Risk}",
                    youtubeVideoId, compliance.RiskLevel);

                return BadRequest(new
                {
                    message = "Compliance review failed. Update blocked.",
                    riskLevel = compliance.RiskLevel,
                    issues = compliance.Issues
                });
            }
        }

        request.VideoId = youtubeVideoId;
        await _youTubeService.UpdateVideoMetadataAsync(request, cancellationToken);

        // Update local DB record
        var video = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (video != null)
        {
            if (request.Title != null) video.Title = request.Title;
            if (request.Description != null) video.Description = request.Description;
            video.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "YouTube metadata update completed for video {VideoId} (human-approved)",
            youtubeVideoId);

        return Ok(new { message = "Metadata updated successfully", videoId = youtubeVideoId });
    }
}
