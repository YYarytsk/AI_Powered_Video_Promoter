using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Application.Services;

/// <summary>
/// Mirrors YouTube (or the mock) into the local database.
///
/// This replaces two near-identical upsert loops that lived in YouTubeController and had already
/// drifted apart - one refreshed the title, the other did not, and neither refreshed the channel
/// row once it existed. Both loops also issued one query per video; this one loads the affected
/// rows in a single query instead.
/// </summary>
public class YouTubeSyncService : IYouTubeSyncService
{
    private readonly IYouTubeService _youTubeService;
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<YouTubeSyncService> _logger;

    public YouTubeSyncService(
        IYouTubeService youTubeService,
        IAppDbContext dbContext,
        ILogger<YouTubeSyncService> logger)
    {
        _youTubeService = youTubeService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<YouTubeVideoDto>> SyncVideosAsync(CancellationToken cancellationToken = default)
    {
        var videos = await _youTubeService.GetVideosAsync(cancellationToken);
        await UpsertVideosAsync(videos, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return videos;
    }

    public async Task<YouTubeSyncResult> SyncChannelAndVideosAsync(CancellationToken cancellationToken = default)
    {
        var channel = await _youTubeService.GetChannelAsync(cancellationToken);
        await UpsertChannelAsync(channel, cancellationToken);

        var videos = await _youTubeService.GetVideosAsync(cancellationToken);
        await UpsertVideosAsync(videos, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Synced channel {ChannelTitle} and {VideoCount} videos",
            channel.ChannelTitle, videos.Count);

        return new YouTubeSyncResult
        {
            ChannelTitle = channel.ChannelTitle,
            VideosSynced = videos.Count
        };
    }

    private async Task UpsertChannelAsync(YouTubeChannelDto channel, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.YouTubeChannels
            .FirstOrDefaultAsync(c => c.ChannelId == channel.ChannelId, cancellationToken);

        if (existing == null)
        {
            await _dbContext.YouTubeChannels.AddAsync(new YouTubeChannel
            {
                ChannelId = channel.ChannelId,
                ChannelTitle = channel.ChannelTitle,
                Description = channel.Description,
                ThumbnailUrl = channel.ThumbnailUrl,
                IsConnected = channel.IsConnected,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            return;
        }

        // OAuth token columns are deliberately untouched - syncing metadata must never clear a
        // stored connection.
        existing.ChannelTitle = channel.ChannelTitle;
        existing.Description = channel.Description;
        existing.ThumbnailUrl = channel.ThumbnailUrl;
        existing.IsConnected = channel.IsConnected;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpsertVideosAsync(
        IReadOnlyList<YouTubeVideoDto> videos,
        CancellationToken cancellationToken)
    {
        if (videos.Count == 0)
        {
            return;
        }

        var ids = videos.Select(v => v.VideoId).ToList();

        var existingById = await _dbContext.YouTubeVideos
            .Where(v => ids.Contains(v.YouTubeVideoId))
            .ToDictionaryAsync(v => v.YouTubeVideoId, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var video in videos)
        {
            if (!existingById.TryGetValue(video.VideoId, out var existing))
            {
                await _dbContext.YouTubeVideos.AddAsync(new YouTubeVideo
                {
                    YouTubeVideoId = video.VideoId,
                    Title = video.Title,
                    Description = video.Description,
                    PublishedAt = video.PublishedAt,
                    ThumbnailUrl = video.ThumbnailUrl,
                    DurationIso8601 = video.DurationIso8601,
                    PrivacyStatus = video.PrivacyStatus,
                    ViewCount = video.ViewCount,
                    LikeCount = video.LikeCount,
                    CommentCount = video.CommentCount,
                    CreatedAt = now
                }, cancellationToken);
                continue;
            }

            // YouTube owns the title and the statistics. The locally curated fields
            // (CompositionName, Mood, Style, TargetAudience, Description) are the user's and are
            // never overwritten by a sync.
            existing.Title = video.Title;
            existing.ViewCount = video.ViewCount;
            existing.LikeCount = video.LikeCount;
            existing.CommentCount = video.CommentCount;
            existing.UpdatedAt = now;
        }
    }
}
