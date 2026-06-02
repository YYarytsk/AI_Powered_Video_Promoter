using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IYouTubeService
{
    Task<YouTubeChannelDto> GetChannelAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken cancellationToken = default);
    Task<YouTubeVideoDto?> GetVideoAsync(string videoId, CancellationToken cancellationToken = default);
    Task UpdateVideoMetadataAsync(UpdateYouTubeVideoMetadataRequest request, CancellationToken cancellationToken = default);
}
