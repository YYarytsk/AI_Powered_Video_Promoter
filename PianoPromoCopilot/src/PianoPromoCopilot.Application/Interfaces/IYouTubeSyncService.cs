using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

/// <summary>
/// Reads from the configured <see cref="IYouTubeService"/> (mock or real) and mirrors the result
/// into the local database. Read-only against YouTube - nothing here writes to the platform.
/// </summary>
public interface IYouTubeSyncService
{
    /// <summary>Fetches the video list and upserts it locally. Returns what YouTube reported.</summary>
    Task<IReadOnlyList<YouTubeVideoDto>> SyncVideosAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches the channel and its videos and upserts both locally.</summary>
    Task<YouTubeSyncResult> SyncChannelAndVideosAsync(CancellationToken cancellationToken = default);
}
