using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
[Route("api/videos/{youtubeVideoId}")]
public class AnalyticsController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IAnalyticsRecommendationService _recommendationService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAppDbContext dbContext,
        IAnalyticsRecommendationService recommendationService,
        ILogger<AnalyticsController> logger)
    {
        _dbContext = dbContext;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>Returns analytics snapshots for a video from the local database.</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(IEnumerable<AnalyticsSnapshotDto>), 200)]
    public async Task<IActionResult> GetAnalytics(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.VideoAnalyticsSnapshots
            .Where(s => s.YouTubeVideoId == youtubeVideoId)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        return Ok(snapshots);
    }

    /// <summary>Creates a mock analytics snapshot for testing purposes.</summary>
    [HttpPost("analytics/mock")]
    [ProducesResponseType(typeof(AnalyticsSnapshotDto), 201)]
    public async Task<IActionResult> CreateMockAnalytics(
        string youtubeVideoId,
        [FromBody] CreateMockAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        var rng = new Random();
        var snapshot = new VideoAnalyticsSnapshot
        {
            YouTubeVideoId = youtubeVideoId,
            SnapshotDate = request.SnapshotDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Views = rng.Next(50, 500),
            Likes = rng.Next(3, 50),
            Comments = rng.Next(0, 20),
            SubscribersGained = rng.Next(0, 10),
            EstimatedMinutesWatched = rng.Next(100, 2000),
            AverageViewDurationSeconds = rng.Next(60, 300),
            AverageViewPercentage = (decimal)rng.Next(25, 75) / 100m,
            Impressions = rng.Next(200, 5000),
            ImpressionClickThroughRate = (decimal)rng.Next(2, 10) / 100m,
            TrafficSource = request.TrafficSource ?? "YT_SEARCH",
            Country = request.Country ?? "US",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.VideoAnalyticsSnapshots.AddAsync(snapshot, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created mock analytics snapshot for video {VideoId}", youtubeVideoId);

        return CreatedAtAction(nameof(GetAnalytics), new { youtubeVideoId }, MapToDto(snapshot));
    }

    /// <summary>Analyzes analytics snapshots and returns AI-powered recommendations.</summary>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(IEnumerable<AnalyticsRecommendationDto>), 200)]
    public async Task<IActionResult> GetRecommendations(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.VideoAnalyticsSnapshots
            .Where(s => s.YouTubeVideoId == youtubeVideoId)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        var recommendations = _recommendationService.GenerateRecommendations(snapshots);

        return Ok(recommendations);
    }

    private static AnalyticsSnapshotDto MapToDto(VideoAnalyticsSnapshot s) => new()
    {
        Id = s.Id,
        YouTubeVideoId = s.YouTubeVideoId,
        SnapshotDate = s.SnapshotDate,
        Views = s.Views,
        Likes = s.Likes,
        Comments = s.Comments,
        SubscribersGained = s.SubscribersGained,
        EstimatedMinutesWatched = s.EstimatedMinutesWatched,
        AverageViewDurationSeconds = s.AverageViewDurationSeconds,
        AverageViewPercentage = s.AverageViewPercentage,
        Impressions = s.Impressions,
        ImpressionClickThroughRate = s.ImpressionClickThroughRate,
        TrafficSource = s.TrafficSource,
        Country = s.Country,
        CreatedAt = s.CreatedAt
    };
}
