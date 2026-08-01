using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Infrastructure.Services;

/// <summary>
/// Mock YouTube service returning realistic sample data.
/// Used when Features__UseMockYouTube=true or YouTube credentials are not configured.
/// </summary>
public class MockYouTubeService : IYouTubeService
{
    private readonly ILogger<MockYouTubeService> _logger;

    public MockYouTubeService(ILogger<MockYouTubeService> logger)
    {
        _logger = logger;
    }

    public Task<YouTubeChannelDto> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockYouTubeService: returning mock channel data");

        return Task.FromResult(new YouTubeChannelDto
        {
            ChannelId = "UCmock_piano_channel_001",
            ChannelTitle = "PianoPromoCopilot Demo Channel",
            Description = "A demo channel featuring original piano compositions for testing PianoPromoCopilot.",
            ThumbnailUrl = "https://placehold.co/88x88/1a1a2e/ffffff?text=Piano",
            IsConnected = false
        });
    }

    public Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockYouTubeService: returning mock videos");

        return Task.FromResult<IReadOnlyList<YouTubeVideoDto>>(BuildCatalogue());
    }

    public Task<YouTubeVideoDto?> GetVideoAsync(string videoId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockYouTubeService: returning mock video {VideoId}", videoId);

        var video = BuildCatalogue().FirstOrDefault(v => v.VideoId == videoId);
        return Task.FromResult(video);
    }

    /// <summary>
    /// One definition of the sample catalogue. The list and the single-video lookup used to hold
    /// separate copies of the same three videos, which is exactly the kind of fixture that drifts
    /// - and mock mode is the path that has to work with zero setup.
    /// </summary>
    private static List<YouTubeVideoDto> BuildCatalogue()
    {
        return new List<YouTubeVideoDto>
        {
            new YouTubeVideoDto
            {
                VideoId = "mock_video_001",
                Title = "Moonlit Reverie - Original Piano Composition",
                Description = "A peaceful original piano composition evoking the quiet beauty of moonlit nights.",
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Moonlit+Reverie",
                DurationIso8601 = "PT4M32S",
                PrivacyStatus = "public",
                ViewCount = 1247,
                LikeCount = 89,
                CommentCount = 23
            },
            new YouTubeVideoDto
            {
                VideoId = "mock_video_002",
                Title = "Storm's Edge - Dramatic Piano Solo",
                Description = "A dramatic original piano composition capturing the intensity of an approaching storm.",
                PublishedAt = DateTime.UtcNow.AddDays(-14),
                ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Storm%27s+Edge",
                DurationIso8601 = "PT5M18S",
                PrivacyStatus = "public",
                ViewCount = 543,
                LikeCount = 67,
                CommentCount = 15
            },
            new YouTubeVideoDto
            {
                VideoId = "mock_video_003",
                Title = "Spring Morning - A Gentle Piano Piece",
                Description = "A light, hopeful original piano composition inspired by the renewal of spring.",
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Spring+Morning",
                DurationIso8601 = "PT3M45S",
                PrivacyStatus = "public",
                ViewCount = 198,
                LikeCount = 34,
                CommentCount = 8
            }
        };
    }

    public Task UpdateVideoMetadataAsync(UpdateYouTubeVideoMetadataRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "MockYouTubeService: UpdateVideoMetadataAsync called for {VideoId} - no actual update performed in mock mode",
            request.VideoId);

        return Task.CompletedTask;
    }
}
