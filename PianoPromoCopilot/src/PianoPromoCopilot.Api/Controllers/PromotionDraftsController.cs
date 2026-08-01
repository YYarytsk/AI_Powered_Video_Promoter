using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
public class PromotionDraftsController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IVideoOptimizationService _optimizationService;
    private readonly ILogger<PromotionDraftsController> _logger;

    public PromotionDraftsController(
        IAppDbContext dbContext,
        IVideoOptimizationService optimizationService,
        ILogger<PromotionDraftsController> logger)
    {
        _dbContext = dbContext;
        _optimizationService = optimizationService;
        _logger = logger;
    }

    /// <summary>Returns all promotion drafts for a video.</summary>
    [HttpGet("api/videos/{youtubeVideoId}/promotion-drafts")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDraftDto>), 200)]
    public async Task<IActionResult> GetDrafts(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var drafts = await _dbContext.PromotionDrafts
            .Where(d => d.YouTubeVideoId == youtubeVideoId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => MapToDto(d))
            .ToListAsync(cancellationToken);

        return Ok(drafts);
    }

    /// <summary>Generates promotion drafts for all platforms using LLM and saves them.</summary>
    [HttpPost("api/videos/{youtubeVideoId}/promotion-drafts")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDraftDto>), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GenerateDrafts(
        string youtubeVideoId,
        [FromBody] GeneratePromotionDraftsRequest request,
        CancellationToken cancellationToken)
    {
        var video = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (video == null)
            return NotFound(new { message = $"Video '{youtubeVideoId}' not found" });

        // Use optimization service to generate promotion content
        var optimizeRequest = new OptimizeVideoRequest
        {
            YouTubeVideoId = youtubeVideoId,
            Title = video.Title,
            Description = video.Description,
            CompositionName = video.CompositionName,
            Mood = video.Mood,
            Style = video.Style,
            TargetAudience = video.TargetAudience
        };

        var optimized = await _optimizationService.OptimizeAsync(optimizeRequest, cancellationToken);
        var compliance = optimized.Compliance;

        // Surface the compliance verdict to the client without changing the response body shape
        Response.Headers["X-Compliance-Risk"] = compliance.RiskLevel;
        Response.Headers["X-Compliance-Safe"] = compliance.IsSafeToUse ? "true" : "false";
        Response.Headers["X-Compliance-Issue-Count"] = compliance.Issues.Length.ToString();

        // Compliance review BEFORE any write - blocked content is never persisted as a draft
        if (compliance.RiskLevel == RiskLevel.Blocked)
        {
            _logger.LogWarning(
                "Compliance review blocked promotion draft generation for video {VideoId}. Risk {RiskLevel}, issues: {Issues}",
                youtubeVideoId, compliance.RiskLevel, string.Join("; ", compliance.Issues));

            return UnprocessableEntity(new
            {
                message = "Compliance review failed. No promotion drafts were saved.",
                riskLevel = compliance.RiskLevel,
                issues = compliance.Issues
            });
        }

        var now = DateTime.UtcNow;

        var platforms = new[]
        {
            ("Instagram", optimized.SocialPosts.Instagram),
            ("TikTok", optimized.SocialPosts.TikTok),
            ("Facebook", optimized.SocialPosts.Facebook),
            ("Reddit", optimized.SocialPosts.Reddit),
            ("X", optimized.SocialPosts.X),
            ("LinkedIn", optimized.SocialPosts.LinkedIn),
            ("Email", optimized.SocialPosts.EmailNewsletter),
        };

        var drafts = new List<PromotionDraft>();
        foreach (var (platform, text) in platforms)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                drafts.Add(new PromotionDraft
                {
                    YouTubeVideoId = youtubeVideoId,
                    Platform = platform,
                    DraftText = text,
                    Status = PromotionStatus.Draft,
                    CreatedAt = now
                });
            }
        }

        // Supersede the previous generation, but never discard a draft the user already acted on
        var supersededDrafts = await _dbContext.PromotionDrafts
            .Where(d => d.YouTubeVideoId == youtubeVideoId && d.Status == PromotionStatus.Draft)
            .ToListAsync(cancellationToken);

        if (supersededDrafts.Count > 0)
        {
            _dbContext.PromotionDrafts.RemoveRange(supersededDrafts);
            _logger.LogInformation(
                "Superseded {Count} untouched promotion drafts for video {VideoId}",
                supersededDrafts.Count, youtubeVideoId);
        }

        await _dbContext.PromotionDrafts.AddRangeAsync(drafts, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated {Count} promotion drafts for video {VideoId} (compliance risk {RiskLevel})",
            drafts.Count, youtubeVideoId, compliance.RiskLevel);

        return CreatedAtAction(
            nameof(GetDrafts),
            new { youtubeVideoId },
            drafts.Select(d => MapToDto(d)));
    }

    /// <summary>Updates a promotion draft's text or status.</summary>
    [HttpPut("api/promotion-drafts/{id:int}")]
    [ProducesResponseType(typeof(PromotionDraftDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDraft(
        int id,
        [FromBody] UpdatePromotionDraftRequest request,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.PromotionDrafts
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (draft == null)
            return NotFound(new { message = $"Promotion draft {id} not found" });

        if (request.DraftText != null) draft.DraftText = request.DraftText;
        if (request.Status != null) draft.Status = request.Status;
        if (request.ScheduledFor.HasValue) draft.ScheduledFor = request.ScheduledFor;
        if (request.Status == PromotionStatus.PostedManually) draft.PostedAt = DateTime.UtcNow;
        draft.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(draft));
    }

    private static PromotionDraftDto MapToDto(PromotionDraft d) => new()
    {
        Id = d.Id,
        YouTubeVideoId = d.YouTubeVideoId,
        Platform = d.Platform,
        DraftText = d.DraftText,
        Status = d.Status,
        ScheduledFor = d.ScheduledFor,
        PostedAt = d.PostedAt,
        CreatedAt = d.CreatedAt
    };
}
