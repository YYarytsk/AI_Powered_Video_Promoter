using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IVideoOptimizationService _optimizationService;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IAppDbContext dbContext,
        IVideoOptimizationService optimizationService,
        ILogger<VideosController> logger)
    {
        _dbContext = dbContext;
        _optimizationService = optimizationService;
        _logger = logger;
    }

    /// <summary>Returns all videos from the local database.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VideoDto>), 200)]
    public async Task<IActionResult> GetVideos(CancellationToken cancellationToken)
    {
        var videos = await _dbContext.YouTubeVideos
            .OrderByDescending(v => v.PublishedAt ?? v.CreatedAt)
            .Select(v => MapToDto(v))
            .ToListAsync(cancellationToken);

        return Ok(videos);
    }

    /// <summary>Returns a single video from the local database by YouTubeVideoId.</summary>
    [HttpGet("{youtubeVideoId}")]
    [ProducesResponseType(typeof(VideoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVideo(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var video = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (video == null)
            return NotFound(new { message = $"Video '{youtubeVideoId}' not found" });

        return Ok(MapToDto(video));
    }

    /// <summary>Creates a manual video record in the local database.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VideoDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateVideo([FromBody] CreateVideoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.YouTubeVideoId))
            return BadRequest(new { message = "YouTubeVideoId is required" });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        var existing = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == request.YouTubeVideoId, cancellationToken);

        if (existing != null)
            return Conflict(new { message = $"Video '{request.YouTubeVideoId}' already exists" });

        var video = new YouTubeVideo
        {
            YouTubeVideoId = request.YouTubeVideoId,
            Title = request.Title,
            Description = request.Description,
            PublishedAt = request.PublishedAt,
            ThumbnailUrl = request.ThumbnailUrl,
            CompositionName = request.CompositionName,
            Mood = request.Mood,
            Style = request.Style,
            TargetAudience = request.TargetAudience,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.YouTubeVideos.AddAsync(video, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetVideo), new { youtubeVideoId = video.YouTubeVideoId }, MapToDto(video));
    }

    /// <summary>Updates manual metadata fields on a video.</summary>
    [HttpPut("{youtubeVideoId}")]
    [ProducesResponseType(typeof(VideoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateVideo(
        string youtubeVideoId,
        [FromBody] UpdateVideoRequest request,
        CancellationToken cancellationToken)
    {
        var video = await _dbContext.YouTubeVideos
            .FirstOrDefaultAsync(v => v.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (video == null)
            return NotFound(new { message = $"Video '{youtubeVideoId}' not found" });

        if (request.Title != null) video.Title = request.Title;
        if (request.Description != null) video.Description = request.Description;
        if (request.CompositionName != null) video.CompositionName = request.CompositionName;
        if (request.Mood != null) video.Mood = request.Mood;
        if (request.Style != null) video.Style = request.Style;
        if (request.TargetAudience != null) video.TargetAudience = request.TargetAudience;
        video.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(video));
    }

    /// <summary>
    /// Optimizes video metadata using LLM and compliance review.
    /// Saves suggestions to database when YouTubeVideoId is provided.
    /// </summary>
    [HttpPost("optimize")]
    [ProducesResponseType(typeof(OptimizeVideoResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> OptimizeVideo(
        [FromBody] OptimizeVideoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        _logger.LogInformation("Optimizing video: {Title} (ID: {Id})", request.Title, request.YouTubeVideoId ?? "not provided");

        var result = await _optimizationService.OptimizeAsync(request, cancellationToken);
        return Ok(result);
    }

    private static VideoDto MapToDto(YouTubeVideo v) => new()
    {
        Id = v.Id,
        YouTubeVideoId = v.YouTubeVideoId,
        Title = v.Title,
        Description = v.Description,
        PublishedAt = v.PublishedAt,
        ThumbnailUrl = v.ThumbnailUrl,
        DurationIso8601 = v.DurationIso8601,
        PrivacyStatus = v.PrivacyStatus,
        ViewCount = v.ViewCount,
        LikeCount = v.LikeCount,
        CommentCount = v.CommentCount,
        CompositionName = v.CompositionName,
        Mood = v.Mood,
        Style = v.Style,
        TargetAudience = v.TargetAudience,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };
}
